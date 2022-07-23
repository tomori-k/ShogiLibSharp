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
        [TestMethod()]
        public async Task UsiEngineTest()
        {

            // 固定値と any の共存はできないっぽい
            // mock.Setup(x => x.SendLine(It.IsAny<string>()))
            // mock.Setup(x => x.SendLine("something"))

            var mock = new Mock<IEngineProcess>();

            Position? pos = null;
            mock.Setup(m => m.SendLine(It.IsAny<string>()))
                .Callback(async (string s) =>
                {
                    Trace.WriteLine($"< {s}");
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

                    }
                    else if (s.StartsWith("go"))
                    {
                        await Task.Delay(1000);
                        mock.Raise(x => x.StdOutReceived += null, CreateBestmoveCommandRandom(pos!));
                        pos = null;
                    }
                    else if (s == "stop")
                    {
                        if (pos != null)
                        {
                            mock.Raise(x => x.StdOutReceived += null, CreateBestmoveCommandRandom(pos));
                        }
                    }
                    else if (s == "quit")
                    {
                        mock.Raise(x => x.Exited += null, null, new EventArgs());
                    }
                });

            var obj = mock.Object;
            obj.StdOutReceived += s =>
            {
                Trace.WriteLine($"  > {s}");
            };

            var engine = new UsiEngine(mock.Object);
            await engine.BeginAsync();
            await engine.IsReadyAsync();
            engine.StartNewGame();
            var (m, p) = await engine.GoAsync(new Position(Position.Hirate), new SearchLimit() { Byoyomi = 1000 });
            engine.Gameover("win");
            await engine.QuitAsync();
        }

        private static string CreateBestmoveCommandRandom(Position pos)
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