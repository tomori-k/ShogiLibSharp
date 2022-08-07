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

            using var cts = new CancellationTokenSource();

            var server = new TestServer(Testcases);
            var serverTask = server.ListenAsync(cts.Token);
            var c1 = new CsaClient(new PlayerFactory(Testcases), options1, cts.Token);
            var c2 = new CsaClient(new PlayerFactory(Testcases), options2, cts.Token);

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

        [TestMethod, Timeout(10000)]
        public async Task RejectTest()
        {
            var options1 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "AbcDefGHjkLmnKpqrsTUvwxyz012345",
                Password = "sakura999",
            };
            var options2 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "--__________AAAAAAAAAAAAAAAAAAA",
                Password = "qawsedrftgyhujikolp",
            };

            using var cts = new CancellationTokenSource();
            var testFactory1 = new RejectPlayer(Testcases.Length);
            var testFactory2 = new PlayerFactory(Testcases);

            var server = new TestServer(Testcases);
            var serverTask = server.ListenAsync(cts.Token);
            var c1 = new CsaClient(testFactory1, options1, cts.Token);
            var c2 = new CsaClient(testFactory2, options2, cts.Token);

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

            Assert.AreEqual(Testcases.Length, testFactory1.RejectedCount);
            Assert.AreEqual(Testcases.Length, testFactory2.RejectedCount);
        }

        [TestMethod, Timeout(10000)]
        public async Task LogoutTest()
        {
            var options1 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "011101110111",
                Password = "a",
            };
            var options2 = new ConnectOptions
            {
                HostName = "localhost",
                Port = 4081,
                UserName = "_______________",
                Password = "b",
            };

            using var cts = new CancellationTokenSource();

            var server = new TestServer(new Testcase[0]);
            var serverTask = server.ListenAsync(cts.Token);
            var c1 = new CsaClient(new PlayerFactory(Testcases), options1, cts.Token);
            var c2 = new CsaClient(new PlayerFactory(Testcases), options2, cts.Token);

            await Task.Delay(1000);

            Assert.IsFalse(c1.ConnectionTask.IsCompleted);
            Assert.IsFalse(c2.ConnectionTask.IsCompleted);

            await Task.WhenAll(c1.LogoutAsync(), c2.LogoutAsync());

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

            public int RejectedCount { get; private set; }

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
                ++RejectedCount;
            }

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

        class RejectPlayer : IPlayerFactory
        {
            int summaryCount = 0;
            int rejectCount;

            public int RejectedCount { get; private set; }

            public RejectPlayer(int rejectCount)
            {
                this.rejectCount = rejectCount;
            }

            public Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct)
            {
                ++summaryCount;
                return Task.FromResult<IPlayer?>(null);
            }

            public bool ContinueLogin()
            {
                return summaryCount < rejectCount;
            }

            public void Rejected(GameSummary summary)
            {
                ++RejectedCount;
            }
        }

        class Player : IPlayer
        {
            int moveCount = 0;
            List<(Move, TimeSpan)> moves;
            Testcase testcase;
            Position position;
            GameSummary summary;
            bool started = false;
            bool ended = false;

            public Player(Testcase testcase, GameSummary summary)
            {
                this.testcase = testcase;
                this.position = summary.StartPos!.Clone();
                this.moves = testcase.Moves!;
                this.summary = summary;

                foreach (var (m, _) in summary.Moves!)
                {
                    position.DoMove(m);
                }
            }

            public void GameEnd(EndGameState endState, GameResult result)
            {
                Assert.IsTrue(started);
                Assert.IsFalse(ended);
                Assert.AreEqual(testcase.EndState, endState);
                Assert.AreEqual(testcase.Results![(int)summary.Color], result);
                ended = true;
            }

            public void GameStart()
            {
                Assert.IsFalse(started);
                Assert.IsFalse(ended);
                started = true;
            }

            public void NewMove(Move move, TimeSpan elapsed)
            {
                var (m, t) = moves[moveCount++];
                Assert.IsTrue(started);
                Assert.IsFalse(ended);
                Assert.AreEqual(m, move);
                Assert.AreEqual(t, elapsed);
                position.DoMove(m);
            }

            public async Task<Move> ThinkAsync(Position pos, RemainingTime time, CancellationToken ct)
            {
                if (moveCount >= testcase.Times!.Length) await Task.Delay(-1, ct);
                var expectedTime = testcase.Times![moveCount];
                Assert.IsTrue(started);
                Assert.IsFalse(ended);
                Assert.AreEqual(expectedTime[Color.Black], time[Color.Black]);
                Assert.AreEqual(expectedTime[Color.White], time[Color.White]);
                Assert.AreEqual(position.SfenWithMoves(), pos.SfenWithMoves());
                return moves[moveCount].Item1;
            }
        }

        static readonly Testcase[] Testcases = new[]
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

            // https://golan.sakura.ne.jp/denryusen/dr3_tsec/kifufiles/dr3tsec+buoy_maiyan1_tsec3p2f4-9-top_25_suishoo_burningbridges-300-2F+suishoo+burningbridges+20220724145614.csa
            new Testcase
            {
                SummaryStr = @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220807-Test-1
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:-
Max_Moves:512
BEGIN Time
Time_Unit:1sec
Increment:2
Total_Time:300
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
+2726FU,T2
-3334FU,T2
+7776FU,T2
-4344FU,T2
+2625FU,T2
-2233KA,T2
+9796FU,T2
-8242HI,T2
+9695FU,T2
-5162OU,T2
+3948GI,T2
-6272OU,T2
+5968OU,T2
-7282OU,T2
+6878OU,T2
-9192KY,T2
+3736FU,T2
-8291OU,T2
+2937KE,T2
-7182GI,T2
+4746FU,T2
-3132GI,T2
+4958KI,T2
-3243GI,T2
+8866KA,T2
END Position
END Game_Summary
",
                Summary = new GameSummary
                {
                    GameId = "20220807-Test-1",
                    StartColor = Color.White,
                    MaxMoves = 512,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromSeconds(1.0),
                        LeastTimePerMove = TimeSpan.Zero,
                        TotalTime = TimeSpan.FromSeconds(300.0),
                        Byoyomi = TimeSpan.Zero,
                        Delay = TimeSpan.Zero,
                        Increment = TimeSpan.FromSeconds(2.0),
                        IsRoundUp = false,
                    },
                    StartPos = new Position(Position.Hirate),
                    Moves = new List<(Move, TimeSpan)>
                    {
                        (Usi.ParseMove("2g2f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3c3d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7g7f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4c4d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2f2e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2b3c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("9g9f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8b4b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("9f9e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5a6b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3i4h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6b7b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5i6h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7b8b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6h7h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("9a9b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3g3f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8b9a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2i3g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7a8b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4g4f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3a3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4i5h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3b4c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8h6f"), TimeSpan.FromSeconds(2.0)),
                    },
                },

                Moves = new List<(Move, TimeSpan)>
                {
                    (Usi.ParseMove("4c5d"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("4f4e"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("4a5a"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("7i8h"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("7c7d"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("6i6h"), TimeSpan.FromSeconds(4.0)),
                    (Usi.ParseMove("5d6e"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("6f5e"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("5c5d"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("5e7g"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("7d7e"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("7f7e"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("6e7f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("7g8f"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("7f8e"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("2e2d"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("2c2d"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("8f7g"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("8e7f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("7g8f"), TimeSpan.FromSeconds(3.0)),
                    (Usi.ParseMove("7f8e"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("8f7g"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("8e7f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("7g8f"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("7f8e"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("8f7g"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("8e7f"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("7g8f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("7f8e"), TimeSpan.FromSeconds(3.0)),
                },

                ResultStrs = new[] { "#DRAW", "#DRAW" },
                Results = new[] { GameResult.Draw, GameResult.Draw },
                EndStateStr = "#SENNICHITE",
                EndState = EndGameState.Sennichite,
                Times = new[]
                {
                    new RemainingTime(TimeSpan.FromSeconds(300.0), TimeSpan.FromSeconds(300.0)),
                    new RemainingTime(TimeSpan.FromSeconds(300.0), TimeSpan.FromSeconds(292.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(292.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(293.0)),
                    new RemainingTime(TimeSpan.FromSeconds(282.0), TimeSpan.FromSeconds(293.0)),
                    new RemainingTime(TimeSpan.FromSeconds(282.0), TimeSpan.FromSeconds(294.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(294.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(271.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(271.0), TimeSpan.FromSeconds(291.0)),
                    new RemainingTime(TimeSpan.FromSeconds(262.0), TimeSpan.FromSeconds(291.0)),
                    new RemainingTime(TimeSpan.FromSeconds(262.0), TimeSpan.FromSeconds(292.0)),
                    new RemainingTime(TimeSpan.FromSeconds(258.0), TimeSpan.FromSeconds(292.0)),
                    new RemainingTime(TimeSpan.FromSeconds(258.0), TimeSpan.FromSeconds(293.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(293.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(287.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(287.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(287.0)),
                    new RemainingTime(TimeSpan.FromSeconds(254.0), TimeSpan.FromSeconds(287.0)),
                    new RemainingTime(TimeSpan.FromSeconds(254.0), TimeSpan.FromSeconds(288.0)),
                    new RemainingTime(TimeSpan.FromSeconds(251.0), TimeSpan.FromSeconds(288.0)),
                    new RemainingTime(TimeSpan.FromSeconds(251.0), TimeSpan.FromSeconds(289.0)),
                    new RemainingTime(TimeSpan.FromSeconds(248.0), TimeSpan.FromSeconds(289.0)),
                    new RemainingTime(TimeSpan.FromSeconds(248.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(244.0), TimeSpan.FromSeconds(290.0)),
                    new RemainingTime(TimeSpan.FromSeconds(244.0), TimeSpan.FromSeconds(282.0)),
                    new RemainingTime(TimeSpan.FromSeconds(244.0), TimeSpan.FromSeconds(282.0)),
                }
            },

            // https://golan.sakura.ne.jp/denryusen/dr3_tsec/kifufiles/dr3tsec+buoy_noda1_tsec3p1f3-4-bottom_92_suishoo_nibanshibori-300-2F+suishoo+nibanshibori+20220724084348.csa
            new Testcase
            {
                SummaryStr = @"
BEGIN Game_Summary
Protocol_Version:1.2
Protocol_Mode:Server
Format:Shogi 1.0
Declaration:Jishogi 1.1
Game_ID:20220807-Test-2
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:+
Max_Moves:512
BEGIN Time
Time_Unit:1sec
Increment:2
Total_Time:300
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
+7776FU,T2
-8384FU,T2
+2726FU,T2
-3334FU,T2
+2625FU,T2
-8485FU,T2
+6978KI,T2
-8586FU,T2
+8786FU,T2
-8286HI,T2
+2524FU,T2
-2324FU,T2
+2824HI,T2
-4132KI,T2
+2434HI,T2
-2133KE,T2
+0087FU,T2
-8676HI,T2
+3484HI,T2
-0082FU,T2
+0023FU,T2
-3223KI,T2
+0024FU,T2
-3345KE,T2
+7877KI,T2
-2277UM,T2
+8977KE,T2
-2322KI,T2
+0085KA,T2
-7626HI,T2
+8563UM,T2
-4557KE,T2
+6353UM,T2
-2629RY,T2
+5331UM,T2
-0032KI,T2
+8454HI,T2
-0052FU,T2
+5457HI,T2
-3231KI,T2
+0063KE,T2
-5141OU,T2
+6371NK,T2
-0045KE,T2
+5755HI,T2
-4537KE,T2
+7161NK,T2
-3749NK,T2
+5949OU,T2
-0027KA,T2
+4958OU,T2
-2939RY,T2
+0051KI,T2
-4132OU,T2
+7765KE,T2
-3221OU,T2
+0023KE,T2
-2749UM,T2
+5857OU,T2
-3132KI,T2
+0033FU,T2
-3933RY,T2
+0041GI,T2
-0048GI,T2
+5766OU,T2
-3324RY,T2
+4132NG,T2
-2232KI,T2
+5552RY,T2
-2423RY,T2
+0041GI,T2
-0031FU,T2
+0035KI,T2
-2326RY,T2
+6655OU,T2
-2635RY,T2
+5564OU,T2
-3223KI,T2
+0036FU,T2
-3525RY,T2
+6573KE,T2
-2534RY,T2
+0054FU,T2
-0062FU,T2
+5262RY,T2
-0074KI,T2
+6453OU,T2
-7473KI,T2
+0074FU,T2
-7374KI,T2
+3635FU,T2
-3424RY,T2
END Position
END Game_Summary
",
                Summary = new GameSummary
                {
                    GameId = "20220807-Test-2",
                    StartColor = Color.Black,
                    MaxMoves = 512,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromSeconds(1.0),
                        LeastTimePerMove = TimeSpan.Zero,
                        TotalTime = TimeSpan.FromSeconds(300.0),
                        Byoyomi = TimeSpan.Zero,
                        Delay = TimeSpan.Zero,
                        Increment = TimeSpan.FromSeconds(2.0),
                        IsRoundUp = false,
                    },
                    StartPos = new Position(Position.Hirate),
                    Moves = new List<(Move, TimeSpan)>
                    {
                        (Usi.ParseMove("7g7f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8c8d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2g2f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3c3d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2f2e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8d8e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6i7h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8e8f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8g8f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8b8f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2e2d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2c2d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2h2d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4a3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2d3d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2a3c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*8g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8f7f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3d8d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*8b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*2c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3b2c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*2d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3c4e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7h7g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2b7g+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8i7g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2c2b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("B*8e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7f2f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8e6c+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4e5g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6c5c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2f2i+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5c3a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("G*3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("8d5d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*5b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5d5g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3b3a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("N*6c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5a4a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6c7a+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("N*4e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5g5e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4e3g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7a6a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3g4i+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5i4i"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("B*2g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4i5h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2i3i"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("G*5a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4a3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7g6e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3b2a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("N*2c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2g4i+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5h5g"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3a3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*3c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3i3c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("S*4a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("S*4h"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5g6f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3c2d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("4a3b+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2b3b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5e5b+"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2d2c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("S*4a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*3a"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("G*3e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2c2f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6f5e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2f3e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5e6d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3b2c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*3f"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3e2e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6e7c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("2e3d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*5d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*6b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("5b6b"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("G*7d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("6d5c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7d7c"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("P*7d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("7c7d"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3f3e"), TimeSpan.FromSeconds(2.0)),
                        (Usi.ParseMove("3d2d"), TimeSpan.FromSeconds(2.0)),
                        
                    },
                },

                Moves = new List<(Move, TimeSpan)>
                {
                    (Usi.ParseMove("5a5b"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("2a1b"), TimeSpan.FromSeconds(48.0)),
                    (Usi.ParseMove("5c4b"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("S*7c"), TimeSpan.FromSeconds(17.0)),
                    (Usi.ParseMove("6b6f"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("N*7e"), TimeSpan.FromSeconds(32.0)),
                    (Usi.ParseMove("7i7h"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("P*7g"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("8h7g"), TimeSpan.FromSeconds(4.0)),
                    (Usi.ParseMove("N*6e"), TimeSpan.FromSeconds(15.0)),
                    (Usi.ParseMove("7g8f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("2d5d"), TimeSpan.FromSeconds(19.0)),
                    (Usi.ParseMove("4b5a"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("7d8e"), TimeSpan.FromSeconds(22.0)),
                    (Usi.ParseMove("8f7e"), TimeSpan.FromSeconds(11.0)),
                    (Usi.ParseMove("8e7e"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("6f7e"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("B*1e"), TimeSpan.FromSeconds(20.0)),
                    (Usi.ParseMove("G*4b"), TimeSpan.FromSeconds(4.0)),
                    (Usi.ParseMove("1e3g+"), TimeSpan.FromSeconds(18.0)),
                    (Usi.ParseMove("P*5e"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("5d5e"), TimeSpan.FromSeconds(12.0)),
                    (Usi.ParseMove("7e6f"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("5e3e"), TimeSpan.FromSeconds(3.0)),
                    (Usi.ParseMove("P*3f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("3g3f"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("6f3f"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("3e3f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("B*5d"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("3f3g"), TimeSpan.FromSeconds(14.0)),
                    (Usi.ParseMove("4b4c"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("1c1d"), TimeSpan.FromSeconds(12.0)),
                    (Usi.ParseMove("N*2e"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("R*2d"), TimeSpan.FromSeconds(26.0)),
                    (Usi.ParseMove("5d8a+"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("2d2e"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("N*3f"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("1b1c"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("P*2d"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("2c2b"), TimeSpan.FromSeconds(10.0)),
                    (Usi.ParseMove("4c4d"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("G*3e"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("8a6c"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("P*4c"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("6c7c"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("3g3f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("4d4c"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("1c2d"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("7c5e"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("2b2c"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("5e6e"), TimeSpan.FromSeconds(3.0)),
                    (Usi.ParseMove("2e2i+"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("S*4f"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("N*3d"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("4f3e"), TimeSpan.FromSeconds(9.0)),
                    (Usi.ParseMove("2d3e"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("6e5e"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("3f4g"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("4c5c"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("S*4f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("5e1a"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("3e3f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("7h6i"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("P*5g"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("1a2a"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("3f3g"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("2a6e"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("N*5f"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("N*6h"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("5f6h+"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("6i6h"), TimeSpan.FromSeconds(8.0)),
                    (Usi.ParseMove("N*5f"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("N*5i"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("4h5i"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("6h5i"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("4i5i"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("S*4e"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("N*4d"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("4e4d"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("5g5h+"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("4d5e"), TimeSpan.FromSeconds(3.0)),
                    (Usi.ParseMove("4f5g+"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("6a6b"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("3g4h"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("N*3e"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("2c2d"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("3e4c"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("2i1i"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("5e6f"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("5g6g"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("5c5d"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("2d3e"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("L*8e"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("3d2f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("6e6d"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("S*2h"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("6f5e"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("2f3h+"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("G*9h"), TimeSpan.FromSeconds(7.0)),
                    (Usi.ParseMove("5f6h+"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("5e4d"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("3e3f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("6d2h"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("1i2h"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("5d5e"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("P*5f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("5e5f"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("4g5f"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("4d5c+"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("4h3i"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("P*6i"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("5i6i"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("S*3g"), TimeSpan.FromSeconds(5.0)),
                    (Usi.ParseMove("3f3g"), TimeSpan.FromSeconds(1.0)),
                    (Usi.ParseMove("P*5i"), TimeSpan.FromSeconds(6.0)),
                    (Usi.ParseMove("5h5i"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("4c3a+"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("B*7g"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("P*7h"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("6i7h"), TimeSpan.FromSeconds(2.0)),
                    (Usi.ParseMove("8e8b+"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("5f5h"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("9h8h"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("7g8h+"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("P*6i"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("5i6i"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("5c4b"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("S*4h"), TimeSpan.FromSeconds(0.0)),
                    (Usi.ParseMove("1g1f"), TimeSpan.FromSeconds(0.0)),
                    (Move.Win, TimeSpan.Zero),
                },

                ResultStrs = new[] { "#LOSE", "#WIN" },
                Results = new[] { GameResult.Lose, GameResult.Win },
                EndStateStr = "#JISHOGI",
                EndState = EndGameState.Jishogi,
                Times = new[]
                {
                    new RemainingTime(TimeSpan.FromSeconds(300.0), TimeSpan.FromSeconds(300.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(300.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(254.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(254.0)),
                    new RemainingTime(TimeSpan.FromSeconds(291.0), TimeSpan.FromSeconds(239.0)),
                    new RemainingTime(TimeSpan.FromSeconds(288.0), TimeSpan.FromSeconds(239.0)),
                    new RemainingTime(TimeSpan.FromSeconds(288.0), TimeSpan.FromSeconds(209.0)),
                    new RemainingTime(TimeSpan.FromSeconds(282.0), TimeSpan.FromSeconds(209.0)),
                    new RemainingTime(TimeSpan.FromSeconds(282.0), TimeSpan.FromSeconds(203.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(203.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(190.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(190.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(173.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(173.0)),
                    new RemainingTime(TimeSpan.FromSeconds(280.0), TimeSpan.FromSeconds(153.0)),
                    new RemainingTime(TimeSpan.FromSeconds(271.0), TimeSpan.FromSeconds(153.0)),
                    new RemainingTime(TimeSpan.FromSeconds(271.0), TimeSpan.FromSeconds(153.0)),
                    new RemainingTime(TimeSpan.FromSeconds(265.0), TimeSpan.FromSeconds(153.0)),
                    new RemainingTime(TimeSpan.FromSeconds(265.0), TimeSpan.FromSeconds(135.0)),
                    new RemainingTime(TimeSpan.FromSeconds(263.0), TimeSpan.FromSeconds(135.0)),
                    new RemainingTime(TimeSpan.FromSeconds(263.0), TimeSpan.FromSeconds(119.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(119.0)),
                    new RemainingTime(TimeSpan.FromSeconds(255.0), TimeSpan.FromSeconds(109.0)),
                    new RemainingTime(TimeSpan.FromSeconds(247.0), TimeSpan.FromSeconds(109.0)),
                    new RemainingTime(TimeSpan.FromSeconds(247.0), TimeSpan.FromSeconds(108.0)),
                    new RemainingTime(TimeSpan.FromSeconds(247.0), TimeSpan.FromSeconds(108.0)),
                    new RemainingTime(TimeSpan.FromSeconds(247.0), TimeSpan.FromSeconds(105.0)),
                    new RemainingTime(TimeSpan.FromSeconds(240.0), TimeSpan.FromSeconds(105.0)),
                    new RemainingTime(TimeSpan.FromSeconds(240.0), TimeSpan.FromSeconds(106.0)),
                    new RemainingTime(TimeSpan.FromSeconds(232.0), TimeSpan.FromSeconds(106.0)),
                    new RemainingTime(TimeSpan.FromSeconds(232.0), TimeSpan.FromSeconds(94.0)),
                    new RemainingTime(TimeSpan.FromSeconds(224.0), TimeSpan.FromSeconds(94.0)),
                    new RemainingTime(TimeSpan.FromSeconds(224.0), TimeSpan.FromSeconds(84.0)),
                    new RemainingTime(TimeSpan.FromSeconds(219.0), TimeSpan.FromSeconds(84.0)),
                    new RemainingTime(TimeSpan.FromSeconds(219.0), TimeSpan.FromSeconds(60.0)),
                    new RemainingTime(TimeSpan.FromSeconds(211.0), TimeSpan.FromSeconds(60.0)),
                    new RemainingTime(TimeSpan.FromSeconds(211.0), TimeSpan.FromSeconds(62.0)),
                    new RemainingTime(TimeSpan.FromSeconds(204.0), TimeSpan.FromSeconds(62.0)),
                    new RemainingTime(TimeSpan.FromSeconds(204.0), TimeSpan.FromSeconds(57.0)),
                    new RemainingTime(TimeSpan.FromSeconds(204.0), TimeSpan.FromSeconds(57.0)),
                    new RemainingTime(TimeSpan.FromSeconds(204.0), TimeSpan.FromSeconds(49.0)),
                    new RemainingTime(TimeSpan.FromSeconds(197.0), TimeSpan.FromSeconds(49.0)),
                    new RemainingTime(TimeSpan.FromSeconds(197.0), TimeSpan.FromSeconds(43.0)),
                    new RemainingTime(TimeSpan.FromSeconds(190.0), TimeSpan.FromSeconds(43.0)),
                    new RemainingTime(TimeSpan.FromSeconds(190.0), TimeSpan.FromSeconds(38.0)),
                    new RemainingTime(TimeSpan.FromSeconds(187.0), TimeSpan.FromSeconds(38.0)),
                    new RemainingTime(TimeSpan.FromSeconds(187.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(180.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(180.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(173.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(173.0), TimeSpan.FromSeconds(35.0)),
                    new RemainingTime(TimeSpan.FromSeconds(172.0), TimeSpan.FromSeconds(35.0)),
                    new RemainingTime(TimeSpan.FromSeconds(172.0), TimeSpan.FromSeconds(36.0)),
                    new RemainingTime(TimeSpan.FromSeconds(166.0), TimeSpan.FromSeconds(36.0)),
                    new RemainingTime(TimeSpan.FromSeconds(166.0), TimeSpan.FromSeconds(29.0)),
                    new RemainingTime(TimeSpan.FromSeconds(159.0), TimeSpan.FromSeconds(29.0)),
                    new RemainingTime(TimeSpan.FromSeconds(159.0), TimeSpan.FromSeconds(31.0)),
                    new RemainingTime(TimeSpan.FromSeconds(153.0), TimeSpan.FromSeconds(31.0)),
                    new RemainingTime(TimeSpan.FromSeconds(153.0), TimeSpan.FromSeconds(33.0)),
                    new RemainingTime(TimeSpan.FromSeconds(147.0), TimeSpan.FromSeconds(33.0)),
                    new RemainingTime(TimeSpan.FromSeconds(147.0), TimeSpan.FromSeconds(33.0)),
                    new RemainingTime(TimeSpan.FromSeconds(141.0), TimeSpan.FromSeconds(33.0)),
                    new RemainingTime(TimeSpan.FromSeconds(141.0), TimeSpan.FromSeconds(34.0)),
                    new RemainingTime(TimeSpan.FromSeconds(135.0), TimeSpan.FromSeconds(34.0)),
                    new RemainingTime(TimeSpan.FromSeconds(135.0), TimeSpan.FromSeconds(34.0)),
                    new RemainingTime(TimeSpan.FromSeconds(129.0), TimeSpan.FromSeconds(34.0)),
                    new RemainingTime(TimeSpan.FromSeconds(129.0), TimeSpan.FromSeconds(35.0)),
                    new RemainingTime(TimeSpan.FromSeconds(123.0), TimeSpan.FromSeconds(35.0)),
                    new RemainingTime(TimeSpan.FromSeconds(123.0), TimeSpan.FromSeconds(36.0)),
                    new RemainingTime(TimeSpan.FromSeconds(118.0), TimeSpan.FromSeconds(36.0)),
                    new RemainingTime(TimeSpan.FromSeconds(118.0), TimeSpan.FromSeconds(37.0)),
                    new RemainingTime(TimeSpan.FromSeconds(112.0), TimeSpan.FromSeconds(37.0)),
                    new RemainingTime(TimeSpan.FromSeconds(112.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(109.0), TimeSpan.FromSeconds(39.0)),
                    new RemainingTime(TimeSpan.FromSeconds(109.0), TimeSpan.FromSeconds(41.0)),
                    new RemainingTime(TimeSpan.FromSeconds(106.0), TimeSpan.FromSeconds(41.0)),
                    new RemainingTime(TimeSpan.FromSeconds(106.0), TimeSpan.FromSeconds(43.0)),
                    new RemainingTime(TimeSpan.FromSeconds(101.0), TimeSpan.FromSeconds(43.0)),
                    new RemainingTime(TimeSpan.FromSeconds(101.0), TimeSpan.FromSeconds(44.0)),
                    new RemainingTime(TimeSpan.FromSeconds(96.0), TimeSpan.FromSeconds(44.0)),
                    new RemainingTime(TimeSpan.FromSeconds(96.0), TimeSpan.FromSeconds(44.0)),
                    new RemainingTime(TimeSpan.FromSeconds(95.0), TimeSpan.FromSeconds(44.0)),
                    new RemainingTime(TimeSpan.FromSeconds(95.0), TimeSpan.FromSeconds(45.0)),
                    new RemainingTime(TimeSpan.FromSeconds(90.0), TimeSpan.FromSeconds(45.0)),
                    new RemainingTime(TimeSpan.FromSeconds(90.0), TimeSpan.FromSeconds(45.0)),
                    new RemainingTime(TimeSpan.FromSeconds(85.0), TimeSpan.FromSeconds(45.0)),
                    new RemainingTime(TimeSpan.FromSeconds(85.0), TimeSpan.FromSeconds(46.0)),
                    new RemainingTime(TimeSpan.FromSeconds(80.0), TimeSpan.FromSeconds(46.0)),
                    new RemainingTime(TimeSpan.FromSeconds(80.0), TimeSpan.FromSeconds(46.0)),
                    new RemainingTime(TimeSpan.FromSeconds(75.0), TimeSpan.FromSeconds(46.0)),
                    new RemainingTime(TimeSpan.FromSeconds(75.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(70.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(70.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(65.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(65.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(60.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(60.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(55.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(55.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(50.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(50.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(46.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(46.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(42.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(42.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(38.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(38.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(34.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(34.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(26.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(26.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(23.0), TimeSpan.FromSeconds(47.0)),
                    new RemainingTime(TimeSpan.FromSeconds(23.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(19.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(19.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(21.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(21.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(23.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(23.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(25.0), TimeSpan.FromSeconds(48.0)),
                    new RemainingTime(TimeSpan.FromSeconds(25.0), TimeSpan.FromSeconds(50.0)),
                    new RemainingTime(TimeSpan.FromSeconds(27.0), TimeSpan.FromSeconds(50.0)),
                    new RemainingTime(TimeSpan.FromSeconds(27.0), TimeSpan.FromSeconds(52.0)),
                    new RemainingTime(TimeSpan.FromSeconds(29.0), TimeSpan.FromSeconds(52.0)),
                    new RemainingTime(TimeSpan.FromSeconds(29.0), TimeSpan.FromSeconds(54.0)),
                    new RemainingTime(TimeSpan.FromSeconds(31.0), TimeSpan.FromSeconds(54.0)),
                    new RemainingTime(TimeSpan.FromSeconds(31.0), TimeSpan.FromSeconds(56.0)),
                    new RemainingTime(TimeSpan.FromSeconds(33.0), TimeSpan.FromSeconds(56.0)),
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
Game_ID:20220807-Test-3
Name+:{0}
Name-:{1}
Your_Turn:{2}
Rematch_On_Draw:NO
To_Move:+
Max_Moves:256
BEGIN Time
Time_Unit:1sec
Total_Time:10
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
                Summary = new GameSummary
                {
                    GameId = "20220807-Test-3",
                    StartColor = Color.Black,
                    MaxMoves = 256,
                    TimeRule = new TimeRule
                    {
                        TimeUnit = TimeSpan.FromSeconds(1.0),
                        LeastTimePerMove = TimeSpan.Zero,
                        TotalTime = TimeSpan.FromSeconds(10.0),
                        Byoyomi = TimeSpan.Zero,
                        Delay = TimeSpan.Zero,
                        Increment = TimeSpan.Zero,
                        IsRoundUp = false,
                    },
                    StartPos = new Position(Position.Hirate),
                    Moves = new List<(Move, TimeSpan)>(),
                },

                Moves = new List<(Move, TimeSpan)>(),

                ResultStrs = new[] { "#LOSE", "#WIN" },
                Results = new[] { GameResult.Lose, GameResult.Win },
                EndStateStr = "#TIME_UP",
                EndState = EndGameState.TimeUp,
                Times = new RemainingTime[0],
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
                        await con.Stream.WriteLineLFAsync($"LOGIN:{con.Name} OK", ct);
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