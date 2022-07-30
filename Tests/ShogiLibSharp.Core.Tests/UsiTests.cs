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
    public class UsiTests
    {
        [TestMethod()]
        public void ParseMoveTest()
        {
            Assert.AreEqual(MoveExtensions.MakeMove(Square.Index(6, 1), Square.Index(5, 1)), Usi.ParseMove("2g2f"));
            Assert.AreEqual(MoveExtensions.MakeMove(Square.Index(7, 7), Square.Index(1, 1), true), Usi.ParseMove("8h2b+"));
            Assert.AreEqual(MoveExtensions.MakeDrop(Piece.Pawn, Square.Index(4, 4)), Usi.ParseMove("P*5e"));
            Assert.AreEqual(Move.Resign, Usi.ParseMove("resign"));
        }
    }
}