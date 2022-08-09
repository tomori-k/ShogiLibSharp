using System;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Csa.Tests
{
    [TestClass()]
    public class ShogiServerClientTests
    {
        [TestMethod(), Timeout(10000)]
        public async Task ShogiServerClientTest()
        {
            var options1 = new ShogiServerOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "zzz",
                Password = "0"
            };
            var options2 = new ShogiServerOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "xyz",
                Password = "1"
            };

            using var cts = new CancellationTokenSource();

            var server = new TestServer(Testcases);
            var serverTask = server.ListenAsync(cts.Token);
            var c1 = new ShogiServerClient(new PlayerFactory(Testcases), options1, cts.Token);
            var c2 = new ShogiServerClient(new PlayerFactory(Testcases), options2, cts.Token);

            var first = await Task.WhenAny(serverTask, c1.ConnectionTask, c2.ConnectionTask);
            if (!first.IsCompletedSuccessfully) await first; // 例外スロー

            if (c1.ConnectionTask.IsCompleted) await c2.ConnectionTask;
            else await c1.ConnectionTask;

            cts.Cancel();
            try
            {
                await serverTask;
            }
            catch (OperationCanceledException) { }
        }

        class PlayerFactory : IPlayerFactory
        {
            int index = 0;
            Testcase[] testcases;

            public PlayerFactory(Testcase[] testcases)
            {
                this.testcases = testcases;
            }

            public bool ContinueLogin()
            {
                return index < testcases.Length;
            }

            public void Rejected(GameSummary summary)
            {
            }

            public Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct)
            {
                if (index >= testcases.Length) return Task.FromResult<IPlayer?>(null);

                var testcase = testcases[index++];

                return Task.FromResult<IPlayer?>(new Player(testcase, summary));
            }
        }

        class Player : IPlayer
        {
            int moveCount = 0;
            List<(string, Move, TimeSpan)> moves;
            Position position;

            public Player(Testcase testcase, GameSummary summary)
            {
                this.position = summary.StartPos!.Clone();
                this.moves = testcase.Moves!;

                foreach (var (m, _) in summary.Moves!)
                {
                    position.DoMove(m);
                }
            }

            public void GameEnd(EndGameState endState, GameResult result)
            {
            }

            public void GameStart()
            {
            }

            public void NewMove(Move move, TimeSpan elapsed)
            {
                var (_, m, _) = moves[moveCount++];
                position.DoMove(m);
            }

            public async Task<(Move, long?, List<Move>?)>
                ThinkAsync(Position pos, RemainingTime time, CancellationToken ct)
            {
                if (moveCount >= moves.Count)
                    await Task.Delay(-1, ct);
                else
                    await Task.Delay(5);
                return (moves[moveCount].Item2,
                    moveCount * 100 * (moveCount % 2 == 0 ? 1 : -1),
                    moves.Select(x => x.Item2).Skip(moveCount).Take(4).ToList());
            }
        }

        record Testcase(
            string SummaryStr,
            GameSummary Summary,
            List<(string, Move, TimeSpan)> Moves,
            string[] ResultStrs,
            string EndStateStr);

        class TestServer
        {
            TcpListener server = new TcpListener(System.Net.IPAddress.Loopback, 4081);
            Testcase[] testcases;

            public TestServer(Testcase[] testcases)
            {
                this.testcases = testcases;
            }

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
                        await con.Stream.WriteLineLFAsync($"##[LOGIN] +OK x1", ct);
                        return true;
                    }
                }
            }

            public async Task ListenAsync(CancellationToken ct)
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
                                while (true)
                                {
                                    var message = await con.Stream.ReadLineAsync(ct);
                                    if (message is null) return false;
                                    if (message == "%%GAME floodgate-300-10F *") break;
                                }
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

                        foreach (var (expectedMessage, move, time) in testcase.Moves!)
                        {
                            while (true)
                            {
                                var message = await connections[(int)pos.Player].Stream.ReadLineAsync(ct);
                                Assert.AreEqual(expectedMessage, message);
                                if (message!.Length > 0)
                                {
                                    if (pos.IsLegalMove(move))
                                    {
                                        pos.DoMove(move);
                                    }
                                    var response = message.Length < 7 ? message : message[0..7];

                                    foreach (var con in connections)
                                        await con.Stream.WriteLineLFAsync($"{response},T{time.TotalSeconds}", ct);

                                    break;
                                }
                            }
                        }

                        foreach (var con in connections)
                            await con.Stream.WriteLineLFAsync(testcase.EndStateStr!, ct);
                        for (int i = 0; i < 2; ++i)
                            await connections[i].Stream.WriteLineLFAsync(testcase.ResultStrs![i], ct);
                    }

                    foreach (var con in connections)
                    {
                        while (true)
                        {
                            var message = await con.Stream.ReadLineAsync(ct);
                            if (message == "LOGOUT")
                            {
                                await con.Stream.WriteLineLFAsync("LOGOUT:completed", ct);
                                break;
                            }
                        }
                    }

                    // キャンセルが来るまで待つ
                    await Task.Delay(-1, ct);
                }
                finally
                {
                    if (connections is not null)
                    {
                        foreach (var c in connections) c.Dispose();
                    }
                    server.Stop();
                }
            }
        }

        class Connection : IDisposable
        {
            TcpClient client;

            public CancellableReaderWriter Stream { get; }
            public string Name { get; set; } = "";
            public string Password { get; set; } = "";
            public string Color { get; set; }

            public Connection(TcpClient client, string color)
            {
                this.client = client;
                this.Stream = new CancellableReaderWriter(client.GetStream());
                this.Color = color;
            }

            public void Dispose()
            {
                Stream.Dispose();
                client.Close();
            }
        }

        static readonly Testcase[] Testcases = new[]
        {
            new Testcase
            (
                @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220809-Test-1
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
Least_Time_Per_Move:0
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
+
+2726FU,T12
-3334FU,T6
END Position
END Game_Summary
",
                new GameSummary
                {
                    GameId = "20220809-Test-1",
                    StartColor = Color.Black,
                    MaxMoves = 1024,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromSeconds(1.0),
                        LeastTimePerMove = TimeSpan.Zero,
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

                new List<(string, Move, TimeSpan)>
                {
                    ("+7776FU,'* 0 +7776FU -4344FU +4746FU -4132KI", Usi.ParseMove("7g7f"), TimeSpan.FromSeconds(30.0)),
                    ("-4344FU,'* -100 -4344FU +4746FU -4132KI", Usi.ParseMove("4c4d"), TimeSpan.FromSeconds(61.0)),
                    ("+4746FU,'* 200 +4746FU -4132KI", Usi.ParseMove("4g4f"), TimeSpan.FromSeconds(1.0)),
                    ("-4132KI,'* -300 -4132KI", Usi.ParseMove("4a3b"), TimeSpan.FromSeconds(1.0)),
                    ("%TORYO", Move.Resign, TimeSpan.FromSeconds(1.0))
                },

                new[] { "#LOSE", "#WIN" },
                "#RESIGN"
            ),

            new Testcase
            (
                @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220809-Test-2
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
+
END Position
END Game_Summary
",
                new GameSummary
                {
                    GameId = "20220809-Test-2",
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

                new List<(string, Move, TimeSpan)>
                {
                    ("+5958OU,'* 0 +5958OU -5152OU", Usi.ParseMove("5i5h"), TimeSpan.FromMilliseconds(100.0 * 200.0)),
                    ("-5152OU,'* -100 -5152OU", Usi.ParseMove("5a5b"), TimeSpan.FromMilliseconds(400.0 * 200.0)),
                },

                new[] { "#CENSORED", "#CENSORED" },
                "#MAX_MOVES"
            ),
        };
    }
}

