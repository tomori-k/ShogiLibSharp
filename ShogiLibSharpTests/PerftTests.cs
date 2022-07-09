using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiLibSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Tests
{
    [TestClass()]
    public class PerftTests
    {
        [TestMethod()]
        public void GoTest()
        {
            var testcases = new[]
            {
                (Position.Hirate, 5, 19861490UL),
                ("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1", 4, 516925165UL),
                ("R8/2K1S1SSk/4B4/9/9/9/9/9/1L1L1L3 b RBGSNLP3g3n17p 1", 3, 53393368UL),
            };
            foreach (var (sfen, depth, expected) in testcases)
            {
                var (nodes, elapsed) = Perft.Go(depth, sfen);
                Trace.WriteLine($"sfen={sfen}, nodes={nodes}, elapsed: {elapsed}");
                Assert.AreEqual(nodes, expected);
            }
        }
    }
}