using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests
{
    [TestClass()]
    public class UsiTests
    {
        [TestMethod()]
        public void ParseMoveTest()
        {
            Assert.AreEqual(MoveExtensions.MakeMove(Square.S27, Square.S26), Usi.ParseMove("2g2f"));
            Assert.AreEqual(MoveExtensions.MakeMove(Square.S88, Square.S22, true), Usi.ParseMove("8h2b+"));
            Assert.AreEqual(MoveExtensions.MakeDrop(Piece.Pawn, Square.S55), Usi.ParseMove("P*5e"));
            Assert.AreEqual(Move.Resign, Usi.ParseMove("resign"));
            Assert.AreEqual(Move.Win, Usi.ParseMove("win"));

            // 例外検知編
            Assert.ThrowsException<FormatException>(() => Usi.ParseMove(""));
            Assert.ThrowsException<FormatException>(() => Usi.ParseMove("9j9a+"));
            Assert.ThrowsException<FormatException>(() => Usi.ParseMove("+7776FU"));
            Assert.ThrowsException<FormatException>(() => Usi.ParseMove("p*2a"));
        }
    }
}