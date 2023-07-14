using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace ShogiLibSharp.Core.Tests
{
    [TestClass()]
    public class PerftTests
    {
        [TestMethod()]
        public void Perft()
        {
            var testcases = new[]
            {
                (Position.Hirate, 5, 19861490UL),
                ("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1", 4, 516925165UL),
                ("R8/2K1S1SSk/4B4/9/9/9/9/9/1L1L1L3 b RBGSNLP3g3n17p 1", 3, 53393368UL),
            };
            foreach (var (sfen, depth, expected) in testcases)
            {
                var pos = new Position(sfen);
                var sw = Stopwatch.StartNew();
                var nodes = PerftImpl(pos, depth);
                var elapsed = sw.Elapsed;
                Trace.WriteLine($"sfen={sfen}, nodes={nodes}, elapsed: {elapsed}");
                Assert.AreEqual(nodes, expected);
            }
        }

        private static ulong PerftImpl(Position pos, int depth)
        {
            var moves = pos.GenerateMoves();

            if (depth == 1)
                return (ulong)moves.Count;

            ulong count = 0;

            foreach (Move m in moves)
            {
                pos.DoMoveUnsafe(m);
                count += PerftImpl(pos, depth - 1);
                pos.TryUndoMove();
            }

            return count;
        }
    }
}