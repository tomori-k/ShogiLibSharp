using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests
{
    [TestClass()]
    public class BitboardTests
    {
        [TestMethod()]
        public void LanceAttacksTest()
        {
            var occ = new Bitboard(
                "o.......o" +
                ".o.....o." +
                "..o...o.." +
                "...o.o..." +
                "....o...." +
                "...o.o..." +
                "..o...o.." +
                ".o.....o." +
                "o.......o");

            var a1 = Bitboard.LanceAttacks(Color.Black, Square.S19, occ);
            var e1 = new Bitboard(
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                ".........");
            Assert.AreEqual(e1, a1, $"\nExpected\n{e1}\nActual\n{a1}");

            var a2 = Bitboard.LanceAttacks(Color.Black, Square.S29, occ);
            var e2 = new Bitboard(
                "........." +
                "........." +
                "........." +
                "........." +
                "........." +
                "........." +
                "........." +
                ".......o." +
                ".........");
            Assert.AreEqual(e2, a2, $"\nExpected\n{e2}\nActual\n{a2}");

            var a3 = Bitboard.LanceAttacks(Color.Black, Square.S87, occ);
            var e3 = new Bitboard(
                "........." +
                ".o......." +
                ".o......." +
                ".o......." +
                ".o......." +
                ".o......." +
                "........." +
                "........." +
                ".........");
            Assert.AreEqual(e3, a3, $"\nExpected\n{e3}\nActual\n{a3}");

            var a4 = Bitboard.LanceAttacks(Color.White, Square.S91, occ);
            var e4 = new Bitboard(
                "........." +
                "o........" +
                "o........" +
                "o........" +
                "o........" +
                "o........" +
                "o........" +
                "o........" +
                "o........");
            Assert.AreEqual(e4, a4, $"\nExpected\n{e4}\nActual\n{a4}");

            var a5 = Bitboard.LanceAttacks(Color.White, Square.S81, occ);
            var e5 = new Bitboard(
                "........." +
                ".o......." +
                "........." +
                "........." +
                "........." +
                "........." +
                "........." +
                "........." +
                ".........");
            Assert.AreEqual(e5, a5, $"\nExpected\n{e5}\nActual\n{a5}");

            var a6 = Bitboard.LanceAttacks(Color.White, Square.S23, occ);
            var e6 = new Bitboard(
                "........." +
                "........." +
                "........." +
                ".......o." +
                ".......o." +
                ".......o." +
                ".......o." +
                ".......o." +
                ".........");
            Assert.AreEqual(e6, a6, $"\nExpected\n{e6}\nActual\n{a6}");
        }

        [TestMethod()]
        public void BishopAttacksTest()
        {
            var occ = new Bitboard(
                "o.......o" +
                ".o.....o." +
                ".oo...o.." +
                "...o.o..." +
                "....o...." +
                "...o.o..." +
                "......o.." +
                ".o.o...o." +
                "o.......o");

            var a1 = Bitboard.BishopAttacks(Square.S55, occ);
            var e1 = new Bitboard(
                "........." +
                "........." +
                "........." +
                "...o.o..." +
                "........." +
                "...o.o..." +
                "........." +
                "........." +
                ".........");
            Assert.AreEqual(e1, a1, $"\nExpected\n{e1}\nActual\n{a1}");

            var a2 = Bitboard.BishopAttacks(Square.S29, occ);
            var e2 = new Bitboard(
                "........." +
                "........." +
                ".o......." +
                "..o......" +
                "...o....." +
                "....o...." +
                ".....o..." +
                "......o.o" +
                ".........");
            Assert.AreEqual(e2, a2, $"\nExpected\n{e2}\nActual\n{a2}");

            var a3 = Bitboard.BishopAttacks(Square.S95, occ);
            var e3 = new Bitboard(
                "........." +
                "........." +
                "..o......" +
                ".o......." +
                "........." +
                ".o......." +
                "..o......" +
                "...o....." +
                ".........");
            Assert.AreEqual(e3, a3, $"\nExpected\n{e3}\nActual\n{a3}");
        }

        [TestMethod()]
        public void RookAttacksTest()
        {
            var occ = new Bitboard(
                "o.......o" +
                ".o.....o." +
                ".oo...o.." +
                "...o.o..." +
                "....o...." +
                "...o.o..." +
                "......o.." +
                ".o.o...o." +
                "o.......o");

            var a1 = Bitboard.RookAttacks(Square.S18, occ);
            var e1 = new Bitboard(
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                "........o" +
                ".......o." +
                "........o");
            Assert.AreEqual(e1, a1, $"\nExpected\n{e1}\nActual\n{a1}");

            var a2 = Bitboard.RookAttacks(Square.S87, occ);
            var e2 = new Bitboard(
                "........." +
                "........." +
                ".o......." +
                ".o......." +
                ".o......." +
                ".o......." +
                "o.ooooo.." +
                ".o......." +
                ".........");
            Assert.AreEqual(e2, a2, $"\nExpected\n{e2}\nActual\n{a2}");

            var a3 = Bitboard.RookAttacks(Square.S55, occ);
            var e3 = new Bitboard(
                "....o...." +
                "....o...." +
                "....o...." +
                "....o...." +
                "oooo.oooo" +
                "....o...." +
                "....o...." +
                "....o...." +
                "....o....");
            Assert.AreEqual(e3, a3, $"\nExpected\n{e3}\nActual\n{a3}");
        }
    }
}
