using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
using ShogiLibSharp.Engine.Options;
using ShogiLibSharp.Engine.Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Tests
{
    [TestClass()]
    public class UsiEngineTests
    {
        [TestMethod(), Timeout(10000)]
        public async Task UsiEngineTest()
        {
            using var engine1 = new UsiEngine(new RandomPlayer());
            using var engine2 = new UsiEngine(new RandomPlayer());

            //engine1.StdIn += s => Trace.WriteLine($"< {s}");
            //engine1.StdOut += s => Trace.WriteLine($"  > {s}");

            await Task.WhenAll(engine1.BeginAsync(), engine2.BeginAsync());
            await Task.WhenAll(engine1.IsReadyAsync(), engine2.IsReadyAsync());
            engine1.StartNewGame();
            engine2.StartNewGame();

            var pos = new Position(Position.Hirate);
            var limits = SearchLimit.Create(TimeSpan.Zero);
            var (p, o) = (engine1, engine2);

            while (!pos.IsMated()
                && pos.CheckRepetition() == Repetition.None)
            {
                var result = await p.GoAsync(pos, limits);
                var bestmove = result.Bestmove;
                var ponder = result.Ponder;
                pos.DoMove(bestmove);
                if (pos.IsLegalMove(ponder))
                {
                    pos.DoMove(ponder);
                    p.GoPonder(pos, limits);
                    pos.UndoMove();
                }
                (p, o) = (o, p);
            }

            await Task.WhenAll(p.StopPonderAsync(), o.StopPonderAsync());

            if (pos.CheckRepetition() == Repetition.None)
            {
                p.Gameover("lose");
                o.Gameover("win");
            }
            else
            {
                p.Gameover("draw");
                o.Gameover("draw");
            }

            await Task.WhenAll(engine1.QuitAsync(), engine2.QuitAsync());
        }

        [TestMethod(), Timeout(5000)]
        public async Task CancelGoTest()
        {
            var process = new RandomPlayer();
            using var engine = new UsiEngine(process);

            await engine.BeginAsync();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await engine.BeginAsync();
            });
            await engine.IsReadyAsync();
            engine.StartNewGame();

            var pos = new Position(Position.Hirate);
            using var cts = new CancellationTokenSource(100);
            await engine.GoAsync(pos, SearchLimit.Create(TimeSpan.FromSeconds(1000000.0)), cts.Token);
        }

        [TestMethod(), Timeout(5000)]
        public async Task GoAsyncTest()
        {
            var process = new RandomPlayer();
            process.StdOutReceived += s => Trace.WriteLine($"> {s}");

            using var engine = new UsiEngine(process);
            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();

            var pos = new Position(Position.Hirate);

            // 1つ前の CancellationTokenSource のキャンセルに反応しないかテスト
            {
                using var cts1 = new CancellationTokenSource();
                await engine.GoAsync(pos, SearchLimit.Create(TimeSpan.FromSeconds(0.1)), cts1.Token);
                using var cts2 = new CancellationTokenSource();
                var task1 = engine.GoAsync(pos, SearchLimit.Create(TimeSpan.FromSeconds(10000.0)), cts2.Token);
                cts1.Cancel();
                await Task.Delay(100);
                Assert.IsFalse(task1.IsCompleted);
                cts2.Cancel();
                await task1;
            }
            // !task0.IsCompleted の間は 、次の Go はできない
            {
                using var cts = new CancellationTokenSource();
                var task0 = engine.GoAsync(pos, SearchLimit.Create(TimeSpan.FromSeconds(1000000.0)), cts.Token);
                await Task.Delay(100);
                Assert.IsFalse(task0.IsCompleted);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                {
                    await engine.GoAsync(pos, SearchLimit.Create(TimeSpan.Zero)); // error!
                });
                cts.Cancel();
                await task0;
            }
        }

        [TestMethod(), Timeout(5000)]
        public async Task UsiOkTimeoutTest()
        {
            using var engine1 = new UsiEngine(CreateMock_FailToReturnUsiOk());
            engine1.UsiOkTimeout = TimeSpan.FromSeconds(0.1);
            await Assert.ThrowsExceptionAsync<EngineException>(async () =>
            {
                await engine1.BeginAsync();
            });

            // 外部の cts のキャンセルは、OperationCanceled になる
            using var engine2 = new UsiEngine(CreateMock_FailToReturnUsiOk());
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
                await engine2.BeginAsync(cts.Token);
            });
        }

        [TestMethod(), Timeout(5000)]
        public async Task ReadyOkTimeoutTest()
        {
            using var engine1 = new UsiEngine(CreateMock_ForgetToReturnReadyOk());
            engine1.ReadyOkTimeout = TimeSpan.FromSeconds(0.1);
            await Assert.ThrowsExceptionAsync<EngineException>(async () =>
            {
                await engine1.BeginAsync();
                await engine1.IsReadyAsync();
            });

            using var engine2 = new UsiEngine(CreateMock_ForgetToReturnReadyOk());
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            {
                await engine2.BeginAsync();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
                await engine2.IsReadyAsync(cts.Token);
            });
        }

        [TestMethod(), Timeout(5000)]
        public async Task QuitTimeoutTest()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock2");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author2");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            // quit を送信しても終了しないエンジン

            mock.Setup(m => m.Kill())
                .Raises(x => x.Exited += null, new EventArgs());

            using var engine = new UsiEngine(mock.Object);
            engine.ExitWaitingTime = TimeSpan.FromSeconds(0.1);

            await engine.BeginAsync();
            await engine.QuitAsync();
        }

        [TestMethod(), Timeout(5000)]
        public async Task BestmoveTimeoutTest()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock3");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author3");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            mock.Setup(m => m.SendLine("isready"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation0...");
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation1...");
                    mock.Raise(x => x.StdOutReceived += null, "readyok");
                });

            using var engine = new UsiEngine(mock.Object);

            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();
            engine.BestmoveResponseTimeout = TimeSpan.FromSeconds(0.2);
            using var cts = new CancellationTokenSource(100);
            await Assert.ThrowsExceptionAsync<EngineException>(async () =>
            {
                await engine.GoAsync(new Position(Position.Hirate), new SearchLimit(), cts.Token);
            });
        }

        [TestMethod, Timeout(5000)]
        public async Task StopPonderTimeoutTest()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock3");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author3");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            mock.Setup(m => m.SendLine("isready"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation0...");
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation1...");
                    mock.Raise(x => x.StdOutReceived += null, "readyok");
                });

            using var engine = new UsiEngine(mock.Object);

            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();

            engine.BestmoveResponseTimeout = TimeSpan.FromSeconds(0.1);
            await Assert.ThrowsExceptionAsync<EngineException>(async () =>
            {
                engine.GoPonder(new Position(Position.Hirate), new SearchLimit());
                await Task.Delay(100);
                await engine.StopPonderAsync();
            });
        }

        [TestMethod, Timeout(1000)]
        public async Task StopPonderAsyncTest()
        {
            using var engine = new UsiEngine(new RandomPlayer());

            //engine.StdIn += s => Trace.WriteLine($"< {s}");
            //engine.StdOut += s => Trace.WriteLine($"  > {s}");

            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();
            engine.GoPonder(new Position(Position.Hirate), new SearchLimit());
            await Task.Delay(100);
            await engine.StopPonderAsync();
            engine.Gameover("win");
            await engine.QuitAsync();
        }

        [TestMethod]
        public async Task DisposeTest()
        {
            var engine1 = new UsiEngine(CreateMock_FailToReturnUsiOk());
            var task = engine1.BeginAsync();
            engine1.Dispose(); // 間違えて Begin 中に Dispose
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task ExitTest()
        {
            using var engine1 = new UsiEngine(CreateMock_ExitWhileIsReady());
            await engine1.BeginAsync();
            await Assert.ThrowsExceptionAsync<EngineException>(async () =>
            {
                await engine1.IsReadyAsync();
            });
            // todo: 探索中に落ちるモック作る
        }

        [TestMethod]
        public async Task UsiOptionTest()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock1");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author1");
                    mock.Raise(x => x.StdOutReceived += null, "option name USI_Hash type spin default 1024 min 1 max 33554432");
                    mock.Raise(x => x.StdOutReceived += null, "option name USI_Ponder type check default false");
                    mock.Raise(x => x.StdOutReceived += null, "option name Style type combo default Normal var Solid var Normal var Risky");
                    mock.Raise(x => x.StdOutReceived += null, "option name BookFile type string default public.bin");
                    mock.Raise(x => x.StdOutReceived += null, "option name LearningFile type filename default <empty>");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            var engine1 = new UsiEngine(mock.Object);
            await engine1.BeginAsync();

            Assert.AreEqual(1024L, ((Spin)engine1.Options["USI_Hash"]).Value);
            Assert.AreEqual(1024L, ((Spin)engine1.Options["USI_Hash"]).Default);
            Assert.AreEqual(1L, ((Spin)engine1.Options["USI_Hash"]).Min);
            Assert.AreEqual(33554432L, ((Spin)engine1.Options["USI_Hash"]).Max);
            Assert.AreEqual(false, ((Check)engine1.Options["USI_Ponder"]).Value);
            Assert.AreEqual(false, ((Check)engine1.Options["USI_Ponder"]).Default);
            Assert.AreEqual("Normal", ((Combo)engine1.Options["Style"]).Value);
            Assert.AreEqual("Solid Normal Risky", string.Join(' ', ((Combo)engine1.Options["Style"]).Items));
            Assert.AreEqual("Normal", ((Combo)engine1.Options["Style"]).Default);
            Assert.AreEqual("public.bin", ((Options.String)engine1.Options["BookFile"]).Value);
            Assert.AreEqual("public.bin", ((Options.String)engine1.Options["BookFile"]).Default);
            Assert.AreEqual("", ((FileName)engine1.Options["LearningFile"]).Value);
            Assert.AreEqual("", ((FileName)engine1.Options["LearningFile"]).Default);
        }

        [TestMethod, Timeout(3000)]
        public void DeadlockTest()
        {
            using var engine = new UsiEngine(new RandomPlayer());
            engine.BeginAsync().Wait();
            engine.IsReadyAsync().Wait();
            engine.StartNewGame();
            engine.GoAsync(new Position(Position.Hirate), SearchLimit.Create(TimeSpan.FromMilliseconds(100.0))).Wait();
            engine.GoPonder(new Position(Position.Hirate), SearchLimit.Create(TimeSpan.FromMilliseconds(100.0)));
            engine.StopPonderAsync().Wait();
            engine.Gameover("win");
            engine.QuitAsync().Wait();
        }

        [TestMethod]
        public async Task InfoCatchTest()
        {
            var mock1 = new Mock<IEngineProcess>();
            mock1.Setup(m => m.SendLine(It.IsAny<string>()))
                .Callback((string s) =>
                {
                    if (s == "usi")
                    {
                        mock1.Raise(x => x.StdOutReceived += null, "id name Mock1000");
                        mock1.Raise(x => x.StdOutReceived += null, "id author Author1000");
                        mock1.Raise(x => x.StdOutReceived += null, "usiok");
                    }
                    else if (s == "isready")
                    {
                        mock1.Raise(x => x.StdOutReceived += null, "readyok");
                    }
                    else if (s == "quit")
                    {
                        mock1.Raise(x => x.Exited += null, new EventArgs());
                    }
                    else if (s.StartsWith("go") || s.StartsWith("ponderhit"))
                    {
                        mock1.Raise(x => x.StdOutReceived += null, "info string a");
                    }
                    else if (s.StartsWith("stop"))
                    {
                        mock1.Raise(x => x.StdOutReceived += null, "info string b");
                        mock1.Raise(x => x.StdOutReceived += null, "bestmove resign");
                    }
                });

            using var engine1 = new UsiEngine(mock1.Object);
            await engine1.BeginAsync();
            await engine1.IsReadyAsync();
            engine1.StartNewGame();

            {
                var pos = new Position(Position.Hirate);
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                var result1 = await engine1.GoAsync(pos, new SearchLimit(), cts.Token);
                Assert.AreEqual(2, result1.InfoList.Count);

                var result2 = await engine1.GoAsync(pos, new SearchLimit(), cts.Token);
                Assert.AreEqual(2, result2.InfoList.Count);

                engine1.GoPonder(pos, new SearchLimit());
                var result3 = await engine1.GoAsync(pos, new SearchLimit(), cts.Token);
                Assert.AreEqual(3, result3.InfoList.Count);

                using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100.0));
                engine1.GoPonder(pos, new SearchLimit());
                pos.DoMove(Usi.ParseMove("7g7f"));
                var result4 = await engine1.GoAsync(pos, new SearchLimit(), cts2.Token);
                Assert.AreEqual(2, result4.InfoList.Count);
            }

            var mock2 = new Mock<IEngineProcess>();
            mock2.Setup(m => m.SendLine(It.IsAny<string>()))
                .Callback((string s) =>
                {
                    if (s == "usi")
                    {
                        mock2.Raise(x => x.StdOutReceived += null, "id name Mock1000");
                        mock2.Raise(x => x.StdOutReceived += null, "id author Author1000");
                        mock2.Raise(x => x.StdOutReceived += null, "usiok");
                    }
                    else if (s == "isready")
                    {
                        mock2.Raise(x => x.StdOutReceived += null, "readyok");
                    }
                    else if (s == "quit")
                    {
                        mock2.Raise(x => x.Exited += null, new EventArgs());
                    }
                    else if (s.StartsWith("go ponder"))
                    {
                        mock2.Raise(x => x.StdOutReceived += null, "info string a");
                        mock2.Raise(x => x.StdOutReceived += null, "bestmove resign");
                    }
                });

            using var engine2 = new UsiEngine(mock2.Object);
            await engine2.BeginAsync();
            await engine2.IsReadyAsync();
            engine2.StartNewGame();

            {
                var pos = new Position(Position.Hirate);

                engine2.GoPonder(pos, new SearchLimit());
                var result1 = await engine2.GoAsync(pos, new SearchLimit());
                Assert.AreEqual(1, result1.InfoList.Count);
            }
        }

        private static IEngineProcess CreateMock_FailToReturnUsiOk()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock1");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author1");
                    mock.Raise(x => x.StdOutReceived += null, "usook"); // typo
                });
            return mock.Object;
        }

        private static IEngineProcess CreateMock_ForgetToReturnReadyOk()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock2");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author2");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            mock.Setup(m => m.SendLine("isready"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation0...");
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation1...");
                    // ...
                });
            return mock.Object;
        }

        private static IEngineProcess CreateMock_ExitWhileIsReady()
        {
            var mock = new Mock<IEngineProcess>();
            mock.Setup(m => m.SendLine("usi"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "id name Mock2");
                    mock.Raise(x => x.StdOutReceived += null, "id author Author2");
                    mock.Raise(x => x.StdOutReceived += null, "usiok");
                });

            mock.Setup(m => m.SendLine("isready"))
                .Callback(() =>
                {
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation0...");
                    mock.Raise(x => x.StdOutReceived += null, "info string preparation1...");
                    mock.Raise(x => x.Exited += null, new EventArgs());
                });
            return mock.Object;
        }
    }
}