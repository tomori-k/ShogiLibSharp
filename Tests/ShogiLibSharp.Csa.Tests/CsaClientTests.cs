using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiLibSharp.Core;
using ShogiLibSharp.Csa;
using ShogiLibSharp.Csa.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa.Tests
{
    [TestClass()]
    public class CsaClientTests
    {
        [TestMethod(), Timeout(10000)]
        public async Task ConnectAsyncTest()
        {
            var options1 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "a--c6",
                Password = "abc1234"
            };
            var options2 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "b-c-d",
                Password = "testteste"
            };
            using var c1 = new CsaClient(options1);
            using var c2 = new CsaClient(options2);
            var server = new TestServer();

            using var cts0 = new CancellationTokenSource();
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();
            using var ctsAll = CancellationTokenSource.CreateLinkedTokenSource(cts0.Token, cts1.Token, cts2.Token);

            var serverTask = server.ListenAsync(cts0, ctsAll.Token);
            var clientTask1 = Task.Run(async () =>
            {
                try
                {
                    await c1.ConnectAsync(new PlayerFactory(), ctsAll.Token);
                }
                catch (Exception)
                {
                    cts1.Cancel();
                    throw;
                }
            });
            var clientTask2 = Task.Run(async () =>
            {
                try
                {
                    await c2.ConnectAsync(new PlayerFactory(), ctsAll.Token);
                }
                catch (Exception)
                {
                    cts2.Cancel();
                    throw;
                }
            });

            var all = Task.WhenAll(serverTask, clientTask1, clientTask2);
            try
            {
                await all;
            }
            catch (Exception)
            {
                if (all.Exception is AggregateException e)
                {
                    foreach (var ex in e.InnerExceptions)
                    {
                        if (ex is OperationCanceledException || ex is CsaServerException) continue;
                        Trace.WriteLine(ex);
                    }

                    if (!e.InnerExceptions
                        .All(x => x is OperationCanceledException || x is CsaServerException))
                        throw;
                }
            }
        }

        class PlayerFactory : IPlayerFactory
        {
            int index = 0;

            public Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct)
            {
                if (index >= testcases.Length) return Task.FromResult<IPlayer?>(null);

                var testcase = testcases[index++];
                var testSummary = testcase.Summary!;
                Assert.AreEqual(testSummary.GameId, summary.GameId);
                Assert.AreEqual(testSummary.StartColor, summary.StartColor);
                Assert.AreEqual(testSummary.MaxMoves, summary.MaxMoves);
                Assert.AreEqual(testSummary.TimeRule, summary.TimeRule);
                Assert.AreEqual(testSummary.StartPos!.SfenWithMoves(), summary.StartPos!.SfenWithMoves());
                Assert.IsTrue(testSummary.Moves!.SequenceEqual(summary.Moves!));

                return Task.FromResult<IPlayer?>(new Player(testcase, summary));
            }
        }

        class Player : IPlayer
        {
            int moveCount = 0;
            List<(Move, TimeSpan)> moves;
            Testcase testcase;
            Position position;
            GameSummary summary;

            public Player(Testcase testcase, GameSummary summary)
            {
                this.testcase = testcase;
                this.position = summary.StartPos!.Clone();
                this.moves = summary.Moves!
                    .Concat(testcase.Moves!)
                    .ToList();
                this.summary = summary;
            }

            public void GameEnd(EndGameState endState, GameResult result)
            {
                Assert.AreEqual(testcase.EndState, endState);
                Assert.AreEqual(testcase.Results![(int)summary.Color], result);
            }

            public void GameStart()
            {
            }

            public void NewMove(Move move, TimeSpan elapsed)
            {
                var (m, t) = moves[moveCount++];
                Assert.AreEqual(m, move);
                Assert.AreEqual(t, elapsed);
                position.DoMove(m);
            }

            public Task<Move> ThinkAsync(Position pos, RemainingTime time, CancellationToken ct)
            {
                var expectedTime = testcase.Times![moveCount - summary.Moves!.Count];
                Assert.AreEqual(expectedTime[Color.Black], time[Color.Black]);
                Assert.AreEqual(expectedTime[Color.White], time[Color.White]);
                Assert.AreEqual(position.SfenWithMoves(), pos.SfenWithMoves());
                return Task.FromResult(moves[moveCount].Item1);
            }
        }

        static readonly Testcase[] testcases = new[]
        {
            new Testcase
            {
                SummaryStr = @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220805-Test-1
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:+
Max_Moves:1024
BEGIN Time
Time_Unit:1sec
Total_Time:600
Byoyomi:10
Least_Time_Per_Move:1
END Time
BEGIN Position
P1-KY-KE-GI-KI-OU-KI-GI-KE-KY
P2 * -HI *  *  *  *  * -KA * 
P3-FU-FU-FU-FU-FU-FU-FU-FU-FU
P4 *  *  *  *  *  *  *  *  * 
P5 *  *  *  *  *  *  *  *  * 
P6 *  *  *  *  *  *  *  *  * 
P7+FU+FU+FU+FU+FU+FU+FU+FU+FU
P8 * +KA *  *  *  *  * +HI * 
P9+KY+KE+GI+KI+OU+KI+GI+KE+KY
P+
P-
+
+2726FU,T12
-3334FU,T6
END Position
END Game_Summary
",
                Summary = new GameSummary
                {
                    GameId = "20220805-Test-1",
                    StartColor = Color.Black,
                    MaxMoves = 1024,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromSeconds(1.0),
                        LeastTimePerMove = TimeSpan.FromSeconds(1.0),
                        TotalTime = TimeSpan.FromSeconds(600.0),
                        Byoyomi = TimeSpan.FromSeconds(10.0),
                        Delay = TimeSpan.Zero,
                        Increment = TimeSpan.Zero,
                        IsRoundUp = false,
                    },
                    StartPos = new Position(Position.Hirate),
                    Moves = new List<(Move, TimeSpan)>
                    {
                        (Usi.ParseMove("2g2f"), TimeSpan.FromSeconds(12.0)),
                        (Usi.ParseMove("3c3d"), TimeSpan.FromSeconds(6.0)),
                    },
                },

                Moves = new List<(Move, TimeSpan)>
                {
                    (Usi.ParseMove("7g7f"), TimeSpan.FromSeconds(30.0)),
                    (Usi.ParseMove("4c4d"), TimeSpan.FromSeconds(61.0)),
                    (Usi.ParseMove("4g4f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("4a3b"), TimeSpan.FromSeconds(1.0)),
                    (Move.Resign, TimeSpan.FromSeconds(1.0))
                },

                ResultStrs = new[] { "#LOSE", "#WIN" },
                Results = new[] { GameResult.Lose, GameResult.Win },
                EndStateStr = "#RESIGN",
                EndState = EndGameState.Resign,
                Times = new[]
                {
                    new RemainingTime(TimeSpan.FromSeconds(588.0), TimeSpan.FromSeconds(594.0)),
                    new RemainingTime(TimeSpan.FromSeconds(558.0), TimeSpan.FromSeconds(594.0)),
                    new RemainingTime(TimeSpan.FromSeconds(558.0), TimeSpan.FromSeconds(533.0)),
                    new RemainingTime(TimeSpan.FromSeconds(557.0), TimeSpan.FromSeconds(533.0)),
                    new RemainingTime(TimeSpan.FromSeconds(557.0), TimeSpan.FromSeconds(532.0)),
                }
            },

            new Testcase
            {
                SummaryStr = @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220805-Test-2
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:-
Max_Moves:100
BEGIN Time
Time_Unit:1min
Total_Time:60
Byoyomi:0
Least_Time_Per_Move:0
Increment:5
Delay: 3
Time_Roundup:YES
END Time
BEGIN Position
P1-KY *  *  *  *  *  * -KE-KY
P2 *  *  *  *  * +TO * -KI-OU
P3 *  * -KE-FU * +GI *  *  * 
P4-FU * -FU *  *  *  * +FU-FU
P5 *  *  * +FU *  * +GI-FU * 
P6 * +FU+FU-KA *  * +FU * +FU
P7+FU *  *  *  *  * +KI+GI * 
P8+HI *  *  *  *  *  *  *  * 
P9+KY+KE *  *  *  * -KA+OU+KY
P+00HI00KI
P-00KI00GI00KE00FU00FU00FU00FU00FU
-
END Position
END Game_Summary
",
                Summary = new GameSummary
                {
                    GameId = "20220805-Test-2",
                    StartColor = Color.White,
                    MaxMoves = 100,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromMinutes(1.0),
                        LeastTimePerMove = TimeSpan.Zero,
                        TotalTime = TimeSpan.FromMinutes(60.0),
                        Byoyomi = TimeSpan.Zero,
                        Delay = TimeSpan.FromMinutes(3.0),
                        Increment = TimeSpan.FromMinutes(5.0),
                        IsRoundUp = true,
                    },
                    StartPos = new Position("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1"),
                    Moves = new List<(Move, TimeSpan)>(),
                },

                Moves = new List<(Move, TimeSpan)>
                {
                    (Usi.ParseMove("N*4d"), TimeSpan.FromMinutes(35.0)),
                    (Usi.ParseMove("2d2c+"), TimeSpan.Zero),
                    (Usi.ParseMove("2b2c"), TimeSpan.FromMinutes(35.0)),
                    (Usi.ParseMove("R*1c"), TimeSpan.Zero),
                    (Usi.ParseMove("4d3f"), TimeSpan.FromMinutes(5.0)),
                },

                ResultStrs = new[] { "#WIN", "#LOSE" },
                Results = new[] { GameResult.Win, GameResult.Lose },
                EndStateStr = "#ILLEGAL_MOVE",
                EndState = EndGameState.IllegalMove,
                Times = new[]
                {
                    new RemainingTime(TimeSpan.FromMinutes(60.0), TimeSpan.FromMinutes(60.0)),
                    new RemainingTime(TimeSpan.FromMinutes(60.0), TimeSpan.FromMinutes(30.0)),
                    new RemainingTime(TimeSpan.FromMinutes(65.0), TimeSpan.FromMinutes(30.0)),
                    new RemainingTime(TimeSpan.FromMinutes(65.0), TimeSpan.Zero),
                    new RemainingTime(TimeSpan.FromMinutes(70.0), TimeSpan.Zero),
                }
            },

            new Testcase
            {
                SummaryStr = @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220805-Test-3
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:+
Max_Moves:2
BEGIN Time
Time_Unit:200msec
Total_Time:60000
END Time
BEGIN Position
P1-KY-KE-GI-KI-OU-KI-GI-KE-KY
P2 * -HI *  *  *  *  * -KA * 
P3-FU-FU-FU-FU-FU-FU-FU-FU-FU
P4 *  *  *  *  *  *  *  *  * 
P5 *  *  *  *  *  *  *  *  * 
P6 *  *  *  *  *  *  *  *  * 
P7+FU+FU+FU+FU+FU+FU+FU+FU+FU
P8 * +KA *  *  *  *  * +HI * 
P9+KY+KE+GI+KI+OU+KI+GI+KE+KY
P+
P-
+
END Position
END Game_Summary
",
                Summary = new GameSummary
                {
                    GameId = "20220805-Test-3",
                    StartColor = Color.Black,
                    MaxMoves = 2,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromMilliseconds(200.0),
                        LeastTimePerMove = TimeSpan.Zero,
                        TotalTime = TimeSpan.FromMilliseconds(60000.0 * 200.0),
                        Byoyomi = TimeSpan.Zero,
                        Delay = TimeSpan.Zero,
                        Increment = TimeSpan.Zero,
                        IsRoundUp = false,
                    },
                    StartPos = new Position(Position.Hirate),
                    Moves = new List<(Move, TimeSpan)>(),
                },

                Moves = new List<(Move, TimeSpan)>
                {
                    (Usi.ParseMove("5i5h"), TimeSpan.FromMilliseconds(100.0 * 200.0)),
                    (Usi.ParseMove("5a5b"), TimeSpan.FromMilliseconds(400.0 * 200.0)),
                },

                ResultStrs = new[] { "#CENSORED", "#CENSORED" },
                Results = new[] { GameResult.Censored, GameResult.Censored },
                EndStateStr = "#MAX_MOVES",
                EndState = EndGameState.MaxMoves,
                Times = new[]
                {
                    new RemainingTime(TimeSpan.FromMilliseconds(60000.0 * 200.0), TimeSpan.FromMilliseconds(60000.0 * 200.0)),
                    new RemainingTime(TimeSpan.FromMilliseconds(59900.0 * 200.0), TimeSpan.FromMilliseconds(60000.0 * 200.0)),
                }
            },
        };

        class Testcase
        {
            public string? SummaryStr { get; set; }
            public GameSummary? Summary { get; set; }          // summaryStr -> summary となるか
            public List<(Move, TimeSpan)>? Moves { get; set; } // 投了も含む
            public string[]? ResultStrs { get; set; }
            public GameResult[]? Results { get; set; }         // 先手、後手それぞれの対局結果
            public string? EndStateStr { get; set; }
            public EndGameState EndState { get; set; }         // 対局結果
            public RemainingTime[]? Times { get; set; }        // 各手数での持ち時間の計算結果
        }

        class TestServer
        {
            TcpListener server = new TcpListener(System.Net.IPAddress.Loopback, 4081);

            static async Task<bool> LoginAsync(Connection con, CancellationToken ct)
            {
                while (true)
                {
                    var message = await con.Stream.ReadLineAsync(ct);
                    if (message is null) return false;
                    if (message.StartsWith("LOGIN"))
                    {
                        var sp = message.Split();
                        if (sp.Length < 3) continue;
                        con.Name = sp[1];
                        con.Password = sp[2];
                        await con.Stream.WriteLineLFAsync($"LOGIN:{con.Name} OK", ct);
                        return true;
                    }
                }
            }

            public async Task ListenAsync(CancellationTokenSource cts, CancellationToken ct)
            {
                List<Connection>? connections = null;
                try
                {
                    server.Start();
                    connections = new List<Connection>();
                    var loginTasks = new List<Task<bool>>();
                    for (int i = 0; i < 2; ++i)
                    {
                        var color = i == 0 ? "+" : "-";
                        var client = await server.AcceptTcpClientAsync(ct);
                        var connection = new Connection(client, color);
                        connections.Add(connection);
                        loginTasks.Add(LoginAsync(connection, ct));
                    }

                    var loginOk = (await Task.WhenAll(loginTasks)).All(x => x);
                    if (!loginOk) return;

                    foreach (var testcase in testcases)
                    {
                        var agreeTasks = new List<Task<bool>>();
                        foreach (var con in connections)
                        {
                            agreeTasks.Add(Task.Run(async () =>
                            {
                                await con.Stream.WriteLineLFAsync(
                                    string.Format(testcase.SummaryStr!, connections[0].Name, connections[1].Name, con.Color),
                                    ct);
                                while (true)
                                {
                                    var message = await con.Stream.ReadLineAsync(ct);
                                    if (message is null) return false;
                                    if (message.StartsWith("AGREE")) return true;
                                    if (message.StartsWith("REJECT")) return false;
                                }
                            }));
                        }

                        var agreeOk = (await Task.WhenAll(agreeTasks)).All(x => x);
                        if (!agreeOk)
                        {
                            foreach (var con in connections)
                                await con.Stream.WriteLineLFAsync($"REJECT:{testcase.Summary!.GameId!} by tekitou", ct);
                            continue;
                        }

                        foreach (var con in connections)
                            await con.Stream.WriteLineLFAsync($"START:{testcase.Summary!.GameId!}", ct);

                        var pos = testcase.Summary!.StartPos!.Clone();
                        foreach (var (m, t) in testcase.Summary!.Moves!)
                        {
                            pos.DoMove(m);
                        }

                        foreach (var (move, time) in testcase.Moves!)
                        {
                            var message = await connections[(int)pos.Player].Stream.ReadLineAsync(ct);
                            if (message is null) return;
                            if (pos.IsLegalMove(move))
                            {
                                Assert.AreEqual(move.Csa(pos), message);
                                pos.DoMove(move);
                            }
                            var response = message.Length < 7 ? message : message[0..7];

                            foreach (var con in connections)
                                await con.Stream.WriteLineLFAsync($"{response},T{time.TotalSeconds}", ct);
                        }

                        foreach (var con in connections)
                            await con.Stream.WriteLineLFAsync(testcase.EndStateStr!, ct);
                        for (int i = 0; i < 2; ++i)
                            await connections[i].Stream.WriteLineLFAsync(testcase.ResultStrs![i], ct);
                    }
                }
                finally
                {
                    if (connections is not null)
                    {
                        foreach (var c in connections) c.Dispose();
                    }
                    server.Stop();
                    cts.Cancel(); // クライアントも全部キャンセル
                }
            }
        }

        class Connection : IDisposable
        {
            TcpClient client;
            
            public WrapperStream Stream { get; }
            public string Name { get; set; } = "";
            public string Password { get; set; } = "";
            public string Color { get; set; }

            public Connection(TcpClient client, string color)
            {
                this.client = client;
                this.Stream = new WrapperStream(client.GetStream());
                this.Color = color;
            }

            public void Dispose()
            {
                Stream.Dispose(); 
                client.Close();
            }
        }
    }
}