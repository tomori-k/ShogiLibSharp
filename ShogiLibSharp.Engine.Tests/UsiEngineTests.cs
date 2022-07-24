using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine;
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
            var (process, log) = CreateMockProcess();
            var engine = new UsiEngine(process);

            // プロセス起動
            await engine.BeginAsync();
            try
            {
                await engine.BeginAsync(); // 2回起動しようとしている
                Assert.Fail();
            }
            catch (InvalidOperationException) { /* 成功 */ }

            // isready
            await engine.IsReadyAsync();

            // usinewgame
            engine.StartNewGame();

            // go
            var pos = new Position(Position.Hirate);
            var cts = new CancellationTokenSource(1000);

            // 20秒の探索を1秒キャンセル
            var (m1, p1) = await engine.GoAsync(pos, new SearchLimit() { Byoyomi = 20000 }, cts.Token);

            // ponder（違う局面を go : ponder ハズレ）
            engine.GoPonder(pos, new SearchLimit());
            pos.DoMove(m1);
            await engine.GoAsync(pos, new SearchLimit() { Byoyomi = 100 });

            // ponder（キャンセル）
            engine.GoPonder(pos, new SearchLimit());
            await Task.Delay(100);
            await engine.StopPonderAsync();

            // ponder（同じ局面を go：ponderhit）
            engine.GoPonder(pos, new SearchLimit());
            await engine.GoAsync(pos, new SearchLimit() { Byoyomi = 100 });

            // gameover
            engine.Gameover("win");

            // quit
            await engine.QuitAsync();

            Assert.AreEqual(log.ToString(), 
@"< usi
  > id name Mock0
  > id author Author0
  > usiok
< isready
  > readyok
< usinewgame
< position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 
< go btime 0 wtime 0 byoyomi 20000
< stop
  > bestmove 1g1f ponder 1c1d
< position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 
< go ponder btime 0 wtime 0 byoyomi 0
< stop
  > bestmove 1g1f ponder 1c1d
< position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 1g1f
< go btime 0 wtime 0 byoyomi 100
  > bestmove 1c1d ponder 1f1e
< position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 1g1f
< go ponder btime 0 wtime 0 byoyomi 0
< stop
  > bestmove 1c1d ponder 1f1e
< position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 1g1f
< go ponder btime 0 wtime 0 byoyomi 0
< ponderhit
  > bestmove 1c1d ponder 1f1e
< gameover win
< quit
");
        }

        [TestMethod(), Timeout(5000)]
        public async Task GoAsyncTest()
        {
            var (process, log) = CreateMockProcess();
            var engine = new UsiEngine(process);
            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();

            var pos = new Position(Position.Hirate);

            // 1つ前の CancellationTokenSource のキャンセルに反応しないかテスト
            {
                var cts1 = new CancellationTokenSource();
                var task0 = engine.GoAsync(pos, new SearchLimit() { Byoyomi = 0 }, cts1.Token);
                await Task.Delay(1000);           // task0 が終わるぐらい十分な時間待つ
                Assert.IsTrue(task0.IsCompleted); // 多分終わってる
                var cts2 = new CancellationTokenSource();
                var task1 = engine.GoAsync(pos, new SearchLimit() { Byoyomi = 1000000000 }, cts2.Token);
                cts1.Cancel();         // 2回目の Go に対して、1つ目のキャンセルは無視
                await Task.Delay(100); // 大体 100 ms ぐらい待てば、（キャンセルが効いているなら） bestmove が返ってきてるはず（しかし現在の実装では実際は bestmove を無限に待つ可能性もある → bestmove 待ちのタイムアウトが必要）
                Assert.IsFalse(task1.IsCompleted);
                cts2.Cancel();
                await task0;
                await task1;
            }
            // !task0.IsCompleted の間は 、次の Go はできない
            {
                var cts = new CancellationTokenSource();
                var task0 = engine.GoAsync(pos, new SearchLimit() { Byoyomi = 1000000000 }, cts.Token);
                await Task.Delay(100);
                Assert.IsFalse(task0.IsCompleted);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                {
                    await engine.GoAsync(pos, new SearchLimit() { Byoyomi = 0 }); // error!
                });
                cts.Cancel();
                await task0;
            }
        }

        private static (IEngineProcess, StringBuilder) CreateMockProcess()
        {
            // 固定値と any の共存はできないっぽい
            // mock.Setup(x => x.SendLine(It.IsAny<string>()))
            // mock.Setup(x => x.SendLine("something"))

            var mock = new Mock<IEngineProcess>();
            var log = new StringBuilder();

            Position? pos = null;
            TaskCompletionSource? tcs = null;
            mock.Setup(m => m.SendLine(It.IsAny<string>()))
                .Callback(async (string s) =>
                {
                    log.AppendLine($"< {s}");
                    if (s == "usi")
                    {
                        await Task.Delay(100);
                        mock.Raise(x => x.StdOutReceived += null, "id name Mock0");
                        mock.Raise(x => x.StdOutReceived += null, "id author Author0");
                        mock.Raise(x => x.StdOutReceived += null, "usiok");
                    }
                    else if (s == "isready")
                    {
                        await Task.Delay(1000);
                        mock.Raise(x => x.StdOutReceived += null, "readyok");
                    }
                    else if (s.StartsWith("position"))
                    {
                        var sfen = string.Join(' ', s.Split().Skip(2).Take(4));
                        var moves = s
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .SkipWhile(x => x != "moves")
                            .Skip(1)
                            .Select(x => Usi.ParseMove(x));
                        pos = new Position(sfen);
                        foreach (var m in moves)
                        {
                            pos.DoMove(m);
                        }
                    }
                    else if (s.StartsWith("go ponder"))
                    {
                        tcs = new TaskCompletionSource();
                        await Task.WhenAny(Task.Delay(1000000000/* stop or ponderhit が来るまで待ちたい */), tcs.Task);
                        var command = CreateBestmoveCommand(pos!);
                        pos = null;
                        mock.Raise(x => x.StdOutReceived += null, command);
                    }
                    else if (s.StartsWith("go"))
                    {
                        var byoyomi = int.Parse(s.Split()[6]);
                        tcs = new TaskCompletionSource();
                        await Task.WhenAny(Task.Delay(byoyomi), tcs.Task);
                        var command = CreateBestmoveCommand(pos!);
                        pos = null;
                        mock.Raise(x => x.StdOutReceived += null, command);
                    }
                    else if (s == "stop")
                    {
                        tcs?.SetResult();
                    }
                    else if (s == "quit")
                    {
                        mock.Raise(x => x.Exited += null, null, new EventArgs());
                    }
                    else if (s == "ponderhit")
                    {
                        tcs?.SetResult();
                    }
                });

            var obj = mock.Object;
            obj.StdOutReceived += s =>
            {
                log.AppendLine($"  > {s}");
            };

            return (obj, log);
        }

        private static string CreateBestmoveCommand(Position pos)
        {
            var bestmove = Movegen.GenerateMoves(pos)[0];
            pos.DoMove(bestmove);
            var ponder = Movegen.GenerateMoves(pos).FirstOrDefault(Move.None);
            var message = ponder != Move.None
                ? $"bestmove {bestmove.Usi()} ponder {ponder.Usi()}"
                : $"bestmove {bestmove.Usi()}";
            pos.UndoMove();
            return message;
        }
    }
}