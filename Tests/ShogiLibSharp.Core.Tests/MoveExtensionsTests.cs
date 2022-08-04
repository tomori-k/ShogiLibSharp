using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core.Tests
{
    [TestClass()]
    public class MoveExtensionsTests
    {
        [TestMethod()]
        public void CsaTest()
        {
            {
                var pos = new Position(Position.Hirate);
                Assert.AreEqual("+7776FU", Usi.ParseMove("7g7f").Csa(pos));
                Assert.AreEqual("+2726FU", Usi.ParseMove("2g2f").Csa(pos));
                Assert.AreEqual("+9998KY", Usi.ParseMove("9i9h").Csa(pos));
            }
            {
                var pos = new Position("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1");
                Assert.AreEqual("-1213OU", Usi.ParseMove("1b1c").Csa(pos));
                Assert.AreEqual("-3928UM", Usi.ParseMove("3i2h+").Csa(pos));
                Assert.AreEqual("-0028FU", Usi.ParseMove("P*2h").Csa(pos));
            }
        }
    }
}