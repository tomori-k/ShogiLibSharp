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
    public class UsiCommandTests
    {
        [TestMethod()]
        public void ParseInfoTest()
        {
            {
                var info = UsiCommand.ParseInfo("info depth 30 seldepth 40 time 9000 nodes 123456789012345678 multipv 7 score cp 4091 currmove 7g7f hashfull 1000 nps 0 pv 7g7f 8c8d 2g2f 8d8e");
                Assert.AreEqual(30, info.Depth);
                Assert.AreEqual(40, info.SelDepth);
                Assert.AreEqual(TimeSpan.FromSeconds(9.0), info.Time);
                Assert.AreEqual(123456789012345678UL, info.Nodes);
                Assert.AreEqual(7, info.MultiPv);
                Assert.AreEqual(new Score(4091, false, Bound.Exact), info.Score);
                Assert.AreEqual(Usi.ParseMove("7g7f"), info.CurrMove);
                Assert.AreEqual(1000, info.Hashfull);
                Assert.AreEqual(0UL, info.Nps);
                Assert.IsTrue(new[] { Usi.ParseMove("7g7f"), Usi.ParseMove("8c8d"), Usi.ParseMove("2g2f"), Usi.ParseMove("8d8e"), }.SequenceEqual(info.Pv));
            }

            {
                var info = UsiCommand.ParseInfo("info score mate +31 string 7g7f (70%)");
                Assert.AreEqual(new Score(31, true, Bound.Exact), info.Score);
                Assert.AreEqual("7g7f (70%)", info.String);
            }

            {
                var info = UsiCommand.ParseInfo("info score mate -");
                Assert.AreEqual(new Score(-1, true, Bound.UpperBound), info.Score);
                var info2 = UsiCommand.ParseInfo("info score mate +");
                Assert.AreEqual(new Score(1, true, Bound.LowerBound), info2.Score);
            }
        }
    }
}

