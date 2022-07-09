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

            var a1 = Bitboard.LanceAttacks(Color.Black, Square.Index(8, 0), occ);
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
            Assert.AreEqual(e1, a1);

            var a2 = Bitboard.LanceAttacks(Color.Black, Square.Index(8, 1), occ);
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
            Assert.AreEqual(e2, a2);

            var a3 = Bitboard.LanceAttacks(Color.Black, Square.Index(6, 7), occ);
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
            Assert.AreEqual(e3, a3);

            var a4 = Bitboard.LanceAttacks(Color.White, Square.Index(0, 8), occ);
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
            Assert.AreEqual(e4, a4);

            var a5 = Bitboard.LanceAttacks(Color.White, Square.Index(0, 7), occ);
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
            Assert.AreEqual(e5, a5);

            var a6 = Bitboard.LanceAttacks(Color.White, Square.Index(2, 1), occ);
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
            Assert.AreEqual(e6, a6);
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

            var a1 = Bitboard.BishopAttacks(Square.Index(4, 4), occ);
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
            Assert.AreEqual(e1, a1);

            var a2 = Bitboard.BishopAttacks(Square.Index(8, 1), occ);
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
            Assert.AreEqual(e2, a2);

            var a3 = Bitboard.BishopAttacks(Square.Index(4, 8), occ);
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
            Assert.AreEqual(e3, a3);
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

            var a1 = Bitboard.RookAttacks(Square.Index(7, 0), occ);
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
            Assert.AreEqual(e1, a1);

            var a2 = Bitboard.RookAttacks(Square.Index(6, 7), occ);
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
            Assert.AreEqual(e2, a2);

            var a3 = Bitboard.RookAttacks(Square.Index(4, 4), occ);
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
            Assert.AreEqual(e3, a3);
        }
    }
}
