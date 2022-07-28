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
    public class PositionTests
    {
        [TestMethod()]
        public void SfenWithMovesTest()
        {
            var pos = new Position(Position.Hirate);
            pos.DoMove(Usi.ParseMove("7g7f"));
            pos.DoMove(Usi.ParseMove("8c8d"));
            pos.DoMove(Usi.ParseMove("2g2f"));
            pos.DoMove(Usi.ParseMove("8d8e"));
            pos.DoMove(Usi.ParseMove("8h7g"));
            pos.DoMove(Usi.ParseMove("3c3d"));
            pos.DoMove(Usi.ParseMove("7i6h"));
            pos.DoMove(Usi.ParseMove("2b7g+"));
            pos.DoMove(Usi.ParseMove("6h7g"));
            pos.DoMove(Usi.ParseMove("3a2b"));
            pos.DoMove(Usi.ParseMove("3i4h"));
            Assert.AreEqual("sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 7g7f 8c8d 2g2f 8d8e 8h7g 3c3d 7i6h 2b7g+ 6h7g 3a2b 3i4h", pos.SfenWithMoves());
        }
    }
}