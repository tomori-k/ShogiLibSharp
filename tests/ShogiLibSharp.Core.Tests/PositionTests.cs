using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass]
public class PositionTests
{
    // プロパティ

    [TestMethod]
    public void Player()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(Color.Black, position.Player);

        position.DoMove("2g2f".ToMove());

        Assert.AreEqual(Color.White, position.Player);

        position.TryUndoMove();

        Assert.AreEqual(Color.Black, position.Player);
    }

    [TestMethod]
    public void GamePly()
    {
        var position = new Position { Sfen = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 100" };

        Assert.AreEqual(100, position.GamePly);

        position.DoMove("2g2f".ToMove());

        Assert.AreEqual(101, position.GamePly);

        position.TryUndoMove();

        Assert.AreEqual(100, position.GamePly);
    }

    [TestMethod]
    public void InCheck()
    {
        var position = new Position { Sfen = "3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/4r4/4k4 b G2SN2L4Pb2g2s2n2l8p 1" };
        Assert.IsTrue(position.InCheck);
    }

    [TestMethod]
    public void IsMated()
    {
        var position = new Position { Sfen = "+L2+B5/3S5/k2s3pp/1pppp4/p8/1P3P1N1/Pg1PP3P/2+rKS1r+p1/LN2L4 b B2GS2NLPg3p 1" };
        Assert.IsTrue(position.IsMated);
    }

    static IEnumerable<object[]> RepetitionTestcases()
    {
        yield return new object[] { "5k3/9/9/4B4/9/9/9/9/8K b RB2G2S2N3L6Pr2g2s2nl12p 1", new[] { "5d6c", "4a3b", "6c5d", "3b4a" }, Repetition.Lose };
        yield return new object[] { "9/9/1k4+R2/9/9/9/9/9/8K w RB2G2S2N3L6Pb2g2s2nl12p 1", new[] { "8c8d", "3c3d", "8d8c", "3d3c" }, Repetition.Win };
        yield return new object[] { "9/8r/9/9/7K1/9/9/9/k8 w RB3G2S2N3L7Pbg2snl11p 1", new[] { "1b2b", "R*2d", "2b2d", "2e2d", "R*1b", "2d2e" }, Repetition.Draw };
        yield return new object[] { "9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b2b", "2e3e", "2b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Win };
        yield return new object[] { "9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b4b", "2e3e", "4b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Draw };
        yield return new object[] { "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1", "2g2f 8c8d 2f2e 8d8e 7g7f 4a3b 8h7g 3c3d 7i8h 2b7g+ 8h7g 3a2b 3i3h 2b3c 1g1f 9c9d 3h2g 7c7d 2g2f 7a7b 2f1e B*4e 5i6h 7b7c B*3h 3c2b 2e2d 2c2d 1e2d P*2c 2d1e 4e5d 6i7h 7c6d 4g4f 4c4d 1e2f 7d7e 7f7e 6d7e P*7f 8e8f 8g8f 7e8f P*8c 8f7g 8i7g 8b7b S*6e 5d4c 6e7d 4c5d 7d6e 5d4c 6e7d 4c5d 7d6e 5d4c 6e7d 4c5d 7d6e".Split(), Repetition.Draw };
        yield return new object[] { "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1", "2g2f 8c8d 2f2e 8d8e 7g7f 4a3b 8h7g 3c3d 7i6h 2b7g+ 6h7g 3a2b 3i3h 2b3c 3g3f 7a6b 3h3g 7c7d 6i7h 6b7c 3g4f 7c6d 1g1f 9c9d 9g9f 1c1d 6g6f 7d7e 7f7e 6d7e 2e2d 2c2d P*2e P*7f 7g6h 7e6f 2e2d P*2b 2h2e 3c4d 2e6e 7f7g+ 8i7g 6f7g+ 7h7g B*7d 6e6f 7d4g+ S*3h 4g7d P*4e 4d3c 4f5e 6a5b 7g7f 7d8d 6f6g 8b7b P*7e 5a4a 5g5f P*7d 7e7d 8d7d 7f6f 7d8d P*7e 4a3a 2i3g 8d7c 6f6e 7c6b B*4f 6b7c 7e7d 7c8b 3h4g 3c4b 4i4h 2a3c 1f1e 5c5d 5e5d 8b4f 4g4f B*8i 6g6f P*7g 1e1d 7g7h+ 6h5g 7h7g 4h5h 8i7h+ 1d1c+ 7h8g 2d2c+ 2b2c P*2d 2c2d P*2c 7g7f 6f6h 7f7g 6h6f 7g7f 6f6h 7f7g 6h6f 7g7f 6f6h 7f7g 6h6f".Split(), Repetition.Draw };
        yield return new object[] { "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1", "2g2f 8c8d 2f2e 8d8e 7g7f 4a3b 8h7g 3c3d 7i6h 2b7g+ 6h7g 3a2b 6i7h 2b3c 3i4h 7a6b 4g4f 7c7d 4h4g 1c1d 1g1f 6c6d 5i6h 6b6c 3g3f 8a7c 2i3g 5a4b 2h2i 8b8a 9g9f 9c9d 4i4h 6a6b 6g6f 6c5d 4g5f 4b5b 6h7i 5b4b 7i8h 6d6e 2i6i 6e6f 7g6f P*6e 6f6e 7c6e 5f6e 5d6e 6i6e S*6d 6e6i 7d7e S*5e S*7c N*7d 6b5b 5e6d 7c6d S*6c B*5e B*7g 5e7g+ 8i7g 5b6c B*7b 6c7c 7b8a+ 7e7f 8a9a 7f7g+ 7h7g N*6e 6i6e 6d6e 9a7c 4b3a R*6a 3a2b 6a6e+ B*5i G*7h 5i4h+ 3g4e P*7f 7g7f 4h5g 4e3c+ 2a3c S*4a 3b3a S*3b 3a4a 3b4a R*2h P*6h S*6i G*7i 6i7h+ 7i7h G*3a 6e6b G*4b 7c5e S*4d 5e4d 4c4d N*4c 3a4a S*3a 4a3a 4c3a+ 2b3a L*4c".Split(), Repetition.None };
        yield return new object[] { "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1", "7g7f 8c8d 7i6h 3c3d 6h7g 7a6b 2g2f 3a4b 2f2e 4b3c 3i4h 4a3b 4i5h 7c7d 6i7h 5a4a 5i6i 6a5b 5g5f 5c5d 3g3f 8a7c 6g6f 6c6d 5h6g 1c1d 4g4f 4c4d 8h7i 2b3a 7i6h 3b4c 6i7i 4a3b 4h4g 6b6c 2i3g 8d8e 9g9f 9c9d 1g1f 8b8a 2h2i 3a4b 4g5h 4b3a 2i4i 3c2b 4f4e 4d4e 4i4e 3a5c 4e4i 2a3c P*4e 5c2f 6h5i 2f5c 5h4g 1d1e 1f1e P*1f 5i6h 1f1g+ 4i2i 1a1e P*1c P*1a 3f3e 1g1h 2i2f 1h1i 4g4f 3d3e 2e2d 4c3d 5f5e 2c2d 5e5d 6c5d P*3f 3e3f 2f3f P*3e 4f3e 3d3e 6h3e 5c3e 3f3e B*4f B*6h 4f3e 6h3e R*3i 7i8h 8e8f 7g8f 3i3g+ B*7b 3g3e 7b5d+ 3b2c 1c1b+ 1a1b P*3d 3e3d G*4d 3d3h S*3d 2c1d P*3i 3h2h 4d3c 1e1g+ 3c2b S*6i 7h6h 1d1e N*3h P*3g 6h6i 3g3h+ 5d8a 3h3g P*5h L*5c 4e4d N*6c 4d4c+ 5c5h+ 6i7h N*5e 6g7g 5h6h S*6i 6h6i 4c5b S*6g S*3h 3g3h 8a6c 3h3i R*3e S*2e N*3h 6g7h+ 7g7h 5e6g+ 3d2e 6g7h 8h9h 7h8h 9h9g 2d2e S*2d 1e2d 2b2c 2d2c 6c4e 2c1d 3e3d G*2d 3d2d 1d2d G*3e 2d1d 3h2f 2h2f G*2d 1d1e 2d2e 2f2e 3e2e 1e2e R*3e 2e1f S*2e 1f1e".Split(), Repetition.None };
    }

    [DataTestMethod]
    [DynamicData(nameof(RepetitionTestcases), DynamicDataSourceType.Method)]
    public void RepetitionTest(string sfen, string[] cycle, Repetition expected)
    {
        var pos = new Position { Sfen = sfen };

        for (var i = 0; i < 3; ++i)
        {
            foreach (var m in cycle)
            {
                Assert.AreEqual(Repetition.None, pos.Repetition);

                pos.DoMove(m.ToMove());
            }
        }

        Assert.AreEqual(expected, pos.Repetition);
    }

    [TestMethod]
    public void Sfen_Getter()
    {
        var position = new Position { Sfen = Position.Hirate };
        var moves = "7g7f 8c8d 7i6h 3c3d 6h7g 7a6b 2g2f 3a4b 2f2e 4b3c 3i4h 4a3b 4i5h 7c7d 6i7h 5a4a 5i6i 6a5b 5g5f 5c5d 3g3f 8a7c 6g6f 6c6d 5h6g 1c1d 4g4f 4c4d 8h7i 2b3a 7i6h 3b4c 6i7i 4a3b 4h4g 6b6c 2i3g 8d8e 9g9f 9c9d 1g1f 8b8a 2h2i 3a4b 4g5h 4b3a 2i4i 3c2b 4f4e 4d4e 4i4e 3a5c 4e4i 2a3c P*4e 5c2f 6h5i 2f5c 5h4g 1d1e 1f1e P*1f 5i6h 1f1g+ 4i2i 1a1e P*1c P*1a 3f3e 1g1h 2i2f 1h1i 4g4f 3d3e 2e2d 4c3d 5f5e 2c2d 5e5d 6c5d P*3f 3e3f 2f3f P*3e 4f3e 3d3e 6h3e 5c3e 3f3e B*4f B*6h 4f3e 6h3e R*3i 7i8h 8e8f 7g8f 3i3g+ B*7b 3g3e 7b5d+ 3b2c 1c1b+ 1a1b P*3d 3e3d G*4d 3d3h S*3d 2c1d P*3i 3h2h 4d3c 1e1g+ 3c2b S*6i 7h6h 1d1e N*3h P*3g 6h6i 3g3h+ 5d8a 3h3g P*5h L*5c 4e4d N*6c 4d4c+ 5c5h+ 6i7h N*5e 6g7g 5h6h S*6i 6h6i 4c5b S*6g S*3h 3g3h 8a6c 3h3i R*3e S*2e N*3h 6g7h+ 7g7h 5e6g+ 3d2e 6g7h 8h9h 7h8h 9h9g 2d2e S*2d 1e2d 2b2c 2d2c 6c4e 2c1d 3e3d G*2d 3d2d 1d2d G*3e 2d1d 3h2f 2h2f G*2d 1d1e 2d2e 2f2e 3e2e 1e2e R*3e 2e1f S*2e 1f1e"
            .Split()
            .Select(x => x.ToMove());

        foreach (var move in moves)
        {
            position.DoMove(move);
        }

        Assert.AreEqual("l8/4+P3p/2n6/p1pp5/5+BRSk/PSPP5/KP6+l/1+n7/LN1+l2+p1+p b Prb4g2sn6p 179", position.Sfen);
    }

    [TestMethod]
    public void Sfen_Setter()
    {
        var position = new Position
        {
            Sfen = "l8/4+P3p/2n6/p1pp5/5+BRSk/PSPP5/KP6+l/1+n7/LN1+l2+p1+p b Prb4g2sn6p 179"
        };

        var pieces = new PieceArray();
        var hands = new HandArray();

        hands[0] = (Hand)0x00_00_00_00_00_00_01_00UL;
        hands[1] = (Hand)0x01_01_04_02_01_00_06_00UL;
        pieces[1] = Piece.W_Pawn;
        pieces[4] = Piece.W_King;
        pieces[6] = Piece.W_ProLance;
        pieces[8] = Piece.W_ProPawn;
        pieces[13] = Piece.B_Silver;
        pieces[22] = Piece.B_Rook;
        pieces[26] = Piece.W_ProPawn;
        pieces[31] = Piece.B_ProBishop;
        pieces[37] = Piece.B_ProPawn;
        pieces[48] = Piece.W_Pawn;
        pieces[50] = Piece.B_Pawn;
        pieces[53] = Piece.W_ProLance;
        pieces[56] = Piece.W_Knight;
        pieces[57] = Piece.W_Pawn;
        pieces[59] = Piece.B_Pawn;
        pieces[68] = Piece.B_Silver;
        pieces[69] = Piece.B_Pawn;
        pieces[70] = Piece.W_ProKnight;
        pieces[71] = Piece.B_Knight;
        pieces[72] = Piece.W_Lance;
        pieces[75] = Piece.W_Pawn;
        pieces[77] = Piece.B_Pawn;
        pieces[78] = Piece.B_King;
        pieces[80] = Piece.B_Lance;

        position._pieces.Should().Be(pieces);
        position._hands.Should().Be(hands);
    }

    [TestMethod]
    public void SfenMoves_Getter()
    {
        var pos = new Position { Sfen = Position.Hirate };

        pos.DoMove("7g7f".ToMove());
        pos.DoMove("8c8d".ToMove());
        pos.DoMove("2g2f".ToMove());
        pos.DoMove("8d8e".ToMove());
        pos.DoMove("8h7g".ToMove());
        pos.DoMove("3c3d".ToMove());
        pos.DoMove("7i6h".ToMove());
        pos.DoMove("2b7g+".ToMove());
        pos.DoMove("6h7g".ToMove());
        pos.DoMove("3a2b".ToMove());
        pos.DoMove("3i4h".ToMove());

        Assert.AreEqual("sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 7g7f 8c8d 2g2f 8d8e 8h7g 3c3d 7i6h 2b7g+ 6h7g 3a2b 3i4h", pos.SfenMoves);
    }

    [TestMethod]
    public void SfenMoves_Setter()
    {
        var position = new Position()
        {
            SfenMoves = "sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1 moves 7g7f 8c8d 2g2f 8d8e 8h7g 3c3d 7i6h 2b7g+ 6h7g 3a2b 3i4h"
        };

        position.Sfen.Should().Be("lnsgkg1nl/1r5s1/p1pppp1pp/6p2/1p7/2P4P1/PPSPPPP1P/5S1R1/LN1GKG1NL w Bb 12");
    }

    [TestMethod]
    public void Moves()
    {
        var pos = new Position { Sfen = Position.Hirate };

        pos.DoMove("7g7f".ToMove());
        pos.DoMove("3c3d".ToMove());
        pos.DoMove("8h2b+".ToMove());
        pos.DoMove("3a2b".ToMove());
        pos.DoMove("5i5h".ToMove());
        pos.TryUndoMove();

        pos.Moves.Should().BeEquivalentTo(new[] {
            ("7g7f".ToMove(), Piece.Empty),
            ("3c3d".ToMove(), Piece.Empty),
            ("8h2b+".ToMove(), Piece.W_Bishop),
            ("3a2b".ToMove(), Piece.B_ProBishop)
        });
    }

    [TestMethod]
    public void ZobristHash()
    {
        // todo
        Assert.Fail();
    }

    // インデクサ

    [TestMethod]
    public void SquareTest()
    {
        var position = new Position() { Sfen = Position.Hirate };

        position[Square.S59].Should().Be(Piece.B_King);
    }

    [TestMethod]
    public void SquareRankFileTest()
    {
        var position = new Position() { Sfen = Position.Hirate };

        position[Rank.R9, File.F5].Should().Be(Piece.B_King);
    }

    [TestMethod]
    public void ColorBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(new Bitboard(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "ooooooooo" +
            ".o.....o." +
            "ooooooooo"
            ),
            position[Color.Black]
       );
    }

    [TestMethod]
    public void PieceBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(new Bitboard(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "ooooooooo" +
            "........." +
            "........."
            ),
            position[Piece.B_Pawn]
       );
    }

    [TestMethod]
    public void ColorPieceBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(new Bitboard(

            "........." +
            "........." +
            "ooooooooo" +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........."
            ),
            position[Color.White, Piece.B_Pawn]
       );
    }

    [TestMethod]
    public void OcuppancyBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(new Bitboard(
            "ooooooooo" +
            ".o.....o." +
            "ooooooooo" +
            "........." +
            "........." +
            "........." +
            "ooooooooo" +
            ".o.....o." +
            "ooooooooo"
            ),
            position.Occupancy
       );
    }

    [TestMethod]
    public void Checkers()
    {
        var position = new Position { Sfen = "3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/4r4/4k4 b G2SN2L4Pb2g2s2n2l8p 1" };

        Assert.AreEqual(new(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "....o...." +
            "........."
            ),
            position.Checkers);
    }

    [DataTestMethod]
    [DataRow("knsglgsnl/9/9/4G4/rL2K1N1R/9/9/1S2S2G1/BN2L3b b 9P9p 1",
        "........." +
        "........." +
        "........." +
        "....o...." +
        ".o......." +
        "........." +
        "........." +
        ".......o." +
        ".........")]
    [DataRow("knlg1gsnl/9/9/9/rNKP5/2P6/b8/6b2/2r6 w 2GSNL7P2sl9p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        ".........")]
    public void Pinned(string sfen, string pattern)
    {
        var pos = new Position { Sfen = sfen };

        Assert.AreEqual(new(pattern), pos.Pinned);
    }

    // コンストラクタ

    public void CopyConstructor()
    {
        Assert.Fail();
    }

    // メソッド

    [TestMethod]
    public void Hand()
    {
        var position = new Position { Sfen = Position.Matsuri };

        Assert.AreEqual((Hand)0x00_00_01_01_01_00_05_00UL, position.Hand(Color.White));
    }

    [TestMethod]
    public void SilverBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(
            new(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "..o.o.o.."
            ),
            position.SilverBB(Color.Black)
        );
    }

    [TestMethod]
    public void GoldBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(
            new(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "...ooo..."
            ),
            position.GoldBB(Color.Black)
        );
    }


    [TestMethod]
    public void BishopBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(
            new(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            ".o......." +
            "........."
            ),
            position.BishopBB(Color.Black)
        );
    }

    [TestMethod]
    public void RookBB()
    {
        var position = new Position { Sfen = Position.Hirate };

        Assert.AreEqual(
            new(
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            "........." +
            ".......o." +
            "........."
            ),
            position.RookBB(Color.Black)
        );
    }

    [TestMethod]
    public void DoMove()
    {
        Assert.Fail();
    }

    [DataTestMethod]
    // 1点足りない
    [DataRow("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/9/4k4 b G2SN2L3Prb2g2s2n2l9p 1", false)]
    // 玉...
    [DataRow("3+N5/5G+PB1/+PR+PP1P2+P/4K4/9/9/9/9/4k4 b G2SN2L4Prb2g2s2n2l8p 1", false)]
    // 王手がかかっている
    [DataRow("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/4r4/4k4 b G2SN2L4Pb2g2s2n2l8p 1", false)]
    // 10枚ない
    [DataRow("3+N5/5G+PB1/+PR+P1KP2+P/9/9/9/9/9/4k4 b G2SN2L5Prb2g2s2n2l8p 1", false)]
    // 1点足りない
    [DataRow("1S3K1B1/2+R1+B2S+N/1L2+L1R+N1/9/9/9/9/9/4k4 b G3g2s2n2l18p 1", false)]
    [DataRow("1S3K1B1/2+R1+B2S+N/1L2+L1R+N1/9/9/9/9/9/4k4 b GP3g2s2n2l17p 1", true)]
    [DataRow("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/9/4k4 b G2SN2L4Prb2g2s2n2l8p 1", true)]
    // 後手は27点でいい
    [DataRow("4K4/9/9/9/9/9/nn+r1l1l2/1+s2b1r2/1b1k3s1 w 3G2S2N2L18Pg 1", true)]
    // 1枚足りない
    [DataRow("4K4/9/9/9/9/9/nn+r1l1l2/1+s2b1r2/1b1k3s1 w 4G2S2N2L18P 1", false)]
    public void CanDeclareWinTest(string sfen, bool expected)
    {
        var pos = new Position { Sfen = sfen };
        var actual = pos.CanDeclareWin();

        Assert.AreEqual(expected, actual, pos.ToString());
    }


    [DataTestMethod]
    [DataRow("lnsgkgsnl/1p5b1/p1b1p3p/3p1p3/1rPPK2pr/9/PP2PPPPP/9/LNSG1GSNL b 2p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
        ,
        "........." +
        "........." +
        "........." +
        "...o.o..." +
        ".......o." +
        "........." +
        "........." +
        "........." +
        ".........")]
    [DataRow("lnsgkgsnb/1p5l1/p1b1pp1pp/3p5/2PP4r/9/4PPPPP/5GSNL/K7r b GSPnl3p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
        ,
        "........." +
        ".......o." +
        "o........" +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        ".........")]
    [DataRow("knsglgsnl/9/9/4G4/rL2K1N1R/9/9/1S2S2G1/BN2L3b b 9P9p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
        ,
        "........." +
        "........." +
        "........." +
        "....o...." +
        ".o......." +
        "........." +
        "........." +
        ".......o." +
        ".........")]
    [DataRow("knsglgsnl/9/9/9/r3K3R/9/9/9/BN2L3b b 2GN9P2sl9p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
        ,
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        ".........")]
    [DataRow("knlg1gsnl/9/9/9/rNKP5/2P6/b8/6b2/2r6 b 2GSNL7P2sl9p 1",
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........." +
        "........."
        ,
        "........." +
        "........." +
        "........." +
        "........." +
        ".o......." +
        "..o......" +
        "........." +
        "........." +
        ".........")]
    public void PinnedByTest(string sfen, string byBlackPattern, string byWhitePattern)
    {
        var pos = new Position { Sfen = sfen };
        var pinnedByBlack = pos.PinnedBy(Color.Black);
        var pinnedByWhite = pos.PinnedBy(Color.White);

        Assert.AreEqual(new(byBlackPattern), pinnedByBlack);
        Assert.AreEqual(new(byWhitePattern), pinnedByWhite);
    }
}