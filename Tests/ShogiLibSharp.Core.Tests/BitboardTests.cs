using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass]
public class BitboardTests
{
    [DataTestMethod]
    [DataRow(Color.Black, Square.S19,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........."
    )]
    [DataRow(Color.Black, Square.S29,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        ".......o." +
        "........."
    )]
    [DataRow(Color.Black, Square.S87,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........." +
        ".o......." +
        ".o......." +
        ".o......." +
        ".o......." +
        ".o......." +
        "........." +
        "........." +
        "........."
    )]
    [DataRow(Color.White, Square.S91,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........." +
        "o........" +
        "o........" +
        "o........" +
        "o........" +
        "o........" +
        "o........" +
        "o........" +
        "o........"
    )]
    [DataRow(Color.White, Square.S81,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........." +
        ".o......." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
    )]
    [DataRow(Color.White, Square.S23,
        "o.......o" +
        ".o.....o." +
        "..o...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "..o...o.." +
        ".o.....o." +
        "o.......o"
        ,
        "........." +
        "........." +
        "........." +
        ".......o." +
        ".......o." +
        ".......o." +
        ".......o." +
        ".......o." +
        "........."
    )]
    public void LanceAttacksTest(Color c, Square sq, string occPattern, string expectedPattern)
    {
        var occ = new Bitboard(occPattern);
        var actual = Bitboard.LanceAttacks(c, sq, occ);
        var expected = new Bitboard(expectedPattern);

        Assert.AreEqual(expected, actual);
    }

    [DataTestMethod]
    [DataRow(Square.S55,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
        "........." +
        "........." +
        "........." +
        "...o.o..." +
        "........." +
        "...o.o..." +
        "........." +
        "........." +
        ".........")]
    [DataRow(Square.S29,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
        "........." +
        "........." +
        ".o......." +
        "..o......" +
        "...o....." +
        "....o...." +
        ".....o..." +
        "......o.o" +
        ".........")]
    [DataRow(Square.S95,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
        "........." +
        "........." +
        "..o......" +
        ".o......." +
        "........." +
        ".o......." +
        "..o......" +
        "...o....." +
        ".........")]
    public void BishopAttacksTest(Square sq, string occPattern, string expectedPattern)
    {
        var occ = new Bitboard(occPattern);
        var actual = Bitboard.BishopAttacks(sq, occ);
        var expected = new Bitboard(expectedPattern);

        Assert.AreEqual(expected, actual);
    }

    [DataTestMethod]
    [DataRow(Square.S18,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        "........o" +
        ".......o." +
        "........o")]
    [DataRow(Square.S87,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
         "........." +
        "........." +
        ".o......." +
        ".o......." +
        ".o......." +
        ".o......." +
        "o.ooooo.." +
        ".o......." +
        ".........")]
    [DataRow(Square.S55,
        "o.......o" +
        ".o.....o." +
        ".oo...o.." +
        "...o.o..." +
        "....o...." +
        "...o.o..." +
        "......o.." +
        ".o.o...o." +
        "o.......o"
        ,
        "....o...." +
        "....o...." +
        "....o...." +
        "....o...." +
        "oooo.oooo" +
        "....o...." +
        "....o...." +
        "....o...." +
        "....o....")]
    public void RookAttacksTest(Square sq, string occPattern, string expectedPattern)
    {
        var occ = new Bitboard(occPattern);
        var actual = Bitboard.RookAttacks(sq, occ);
        var expected = new Bitboard(expectedPattern);

        Assert.AreEqual(expected, actual);
    }
}
