using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass]
public class MovegenTests
{
    static IEnumerable<object[]> GenerateMovesTestcases()
    {
        yield return new object[] { Position.Hirate, new[] { "1g1f", "1i1h", "2g2f", "2h1h", "2h3h", "2h4h", "2h5h", "2h6h", "2h7h", "3g3f", "3i3h", "3i4h", "4g4f", "4i3h", "4i4h", "4i5h", "5g5f", "5i4h", "5i5h", "5i6h", "6g6f", "6i5h", "6i6h", "6i7h", "7g7f", "7i6h", "7i7h", "8g8f", "9g9f", "9i9h" } };
        yield return new object[] { Position.Matsuri, new[] { "1b1c", "1d1e", "2a1c", "2a3c", "2b1c", "2b2c", "2b3b", "2b3c", "2e2f", "3i1g+", "3i2h+", "3i4h+", "3i5g+", "6c6d", "6f3c", "6f4d", "6f4h+", "6f5e", "6f5g+", "6f7e", "6f7g+", "6f8d", "6f8h+", "6f9c", "6f9i+", "7c6e", "7c8e", "7d7e", "9a9b", "9a9c", "9d9e", "G*1c", "G*1e", "G*1g", "G*1h", "G*2c", "G*2f", "G*2h", "G*3a", "G*3b", "G*3c", "G*3d", "G*3h", "G*4a", "G*4d", "G*4e", "G*4f", "G*4g", "G*4h", "G*4i", "G*5a", "G*5b", "G*5c", "G*5d", "G*5e", "G*5f", "G*5g", "G*5h", "G*5i", "G*6a", "G*6b", "G*6d", "G*6g", "G*6h", "G*6i", "G*7a", "G*7b", "G*7e", "G*7g", "G*7h", "G*7i", "G*8a", "G*8b", "G*8c", "G*8d", "G*8e", "G*8g", "G*8h", "G*9b", "G*9c", "G*9e", "G*9f", "N*1c", "N*1e", "N*1g", "N*2c", "N*2f", "N*3a", "N*3b", "N*3c", "N*3d", "N*4a", "N*4d", "N*4e", "N*4f", "N*4g", "N*5a", "N*5b", "N*5c", "N*5d", "N*5e", "N*5f", "N*5g", "N*6a", "N*6b", "N*6d", "N*6g", "N*7a", "N*7b", "N*7e", "N*7g", "N*8a", "N*8b", "N*8c", "N*8d", "N*8e", "N*8g", "N*9b", "N*9c", "N*9e", "N*9f", "P*3a", "P*3b", "P*3c", "P*3d", "P*3h", "P*4a", "P*4d", "P*4e", "P*4f", "P*4g", "P*4h", "P*5a", "P*5b", "P*5c", "P*5d", "P*5e", "P*5f", "P*5g", "P*5h", "P*8a", "P*8b", "P*8c", "P*8d", "P*8e", "P*8g", "P*8h", "S*1c", "S*1e", "S*1g", "S*1h", "S*2c", "S*2f", "S*2h", "S*3a", "S*3b", "S*3c", "S*3d", "S*3h", "S*4a", "S*4d", "S*4e", "S*4f", "S*4g", "S*4h", "S*4i", "S*5a", "S*5b", "S*5c", "S*5d", "S*5e", "S*5f", "S*5g", "S*5h", "S*5i", "S*6a", "S*6b", "S*6d", "S*6g", "S*6h", "S*6i", "S*7a", "S*7b", "S*7e", "S*7g", "S*7h", "S*7i", "S*8a", "S*8b", "S*8c", "S*8d", "S*8e", "S*8g", "S*8h", "S*9b", "S*9c", "S*9e", "S*9f", "3i1g", "3i2h", "3i4h", "3i5g", "6f4h", "6f5g", "6f7g", "6f8h", "6f9i" } };
        yield return new object[] { Position.MaxLegalMoves, new[] { "2b1a", "2b1a+", "2b1c", "2b1c+", "2b2a", "2b2a+", "2b3a", "2b3a+", "2b3c", "2b3c+", "3b2a", "3b2a+", "3b2c", "3b2c+", "3b3a", "3b3a+", "3b4a", "3b4a+", "3b4c", "3b4c+", "4i4a+", "4i4b+", "4i4c", "4i4c+", "4i4d", "4i4e", "4i4f", "4i4g", "4i4h", "5b4a", "5b4a+", "5b4c", "5b4c+", "5b5a", "5b5a+", "5b6a", "5b6a+", "5b6c", "5b6c+", "5c1g+", "5c2f+", "5c3a+", "5c3e+", "5c4b+", "5c4d+", "5c6b+", "5c6d+", "5c7a+", "5c7e+", "5c8f+", "5c9g+", "6i6a+", "6i6b+", "6i6c", "6i6c+", "6i6d", "6i6e", "6i6f", "6i6g", "6i6h", "7b6a", "7b6b", "7b6c", "7b7a", "7b7c", "7b8a", "7b8b", "7b8c", "8i8a+", "8i8b+", "8i8c", "8i8c+", "8i8d", "8i8e", "8i8f", "8i8g", "8i8h", "9a1a+", "9a2a+", "9a3a+", "9a4a+", "9a5a+", "9a6a+", "9a7a+", "9a8a+", "9a9b+", "9a9c+", "9a9d+", "9a9e+", "9a9f+", "9a9g+", "9a9h+", "9a9i+", "B*1a", "B*1c", "B*1d", "B*1e", "B*1f", "B*1g", "B*1h", "B*1i", "B*2a", "B*2c", "B*2d", "B*2e", "B*2f", "B*2g", "B*2h", "B*2i", "B*3a", "B*3c", "B*3d", "B*3e", "B*3f", "B*3g", "B*3h", "B*3i", "B*4a", "B*4b", "B*4c", "B*4d", "B*4e", "B*4f", "B*4g", "B*4h", "B*5a", "B*5d", "B*5e", "B*5f", "B*5g", "B*5h", "B*5i", "B*6a", "B*6b", "B*6c", "B*6d", "B*6e", "B*6f", "B*6g", "B*6h", "B*7a", "B*7c", "B*7d", "B*7e", "B*7f", "B*7g", "B*7h", "B*7i", "B*8a", "B*8b", "B*8c", "B*8d", "B*8e", "B*8f", "B*8g", "B*8h", "B*9b", "B*9c", "B*9d", "B*9e", "B*9f", "B*9g", "B*9h", "B*9i", "G*1a", "G*1c", "G*1d", "G*1e", "G*1f", "G*1g", "G*1h", "G*1i", "G*2a", "G*2c", "G*2d", "G*2e", "G*2f", "G*2g", "G*2h", "G*2i", "G*3a", "G*3c", "G*3d", "G*3e", "G*3f", "G*3g", "G*3h", "G*3i", "G*4a", "G*4b", "G*4c", "G*4d", "G*4e", "G*4f", "G*4g", "G*4h", "G*5a", "G*5d", "G*5e", "G*5f", "G*5g", "G*5h", "G*5i", "G*6a", "G*6b", "G*6c", "G*6d", "G*6e", "G*6f", "G*6g", "G*6h", "G*7a", "G*7c", "G*7d", "G*7e", "G*7f", "G*7g", "G*7h", "G*7i", "G*8a", "G*8b", "G*8c", "G*8d", "G*8e", "G*8f", "G*8g", "G*8h", "G*9b", "G*9c", "G*9d", "G*9e", "G*9f", "G*9g", "G*9h", "G*9i", "L*1c", "L*1d", "L*1e", "L*1f", "L*1g", "L*1h", "L*1i", "L*2c", "L*2d", "L*2e", "L*2f", "L*2g", "L*2h", "L*2i", "L*3c", "L*3d", "L*3e", "L*3f", "L*3g", "L*3h", "L*3i", "L*4b", "L*4c", "L*4d", "L*4e", "L*4f", "L*4g", "L*4h", "L*5d", "L*5e", "L*5f", "L*5g", "L*5h", "L*5i", "L*6b", "L*6c", "L*6d", "L*6e", "L*6f", "L*6g", "L*6h", "L*7c", "L*7d", "L*7e", "L*7f", "L*7g", "L*7h", "L*7i", "L*8b", "L*8c", "L*8d", "L*8e", "L*8f", "L*8g", "L*8h", "L*9b", "L*9c", "L*9d", "L*9e", "L*9f", "L*9g", "L*9h", "L*9i", "N*1c", "N*1d", "N*1e", "N*1f", "N*1g", "N*1h", "N*1i", "N*2c", "N*2d", "N*2e", "N*2f", "N*2g", "N*2h", "N*2i", "N*3c", "N*3d", "N*3e", "N*3f", "N*3g", "N*3h", "N*3i", "N*4c", "N*4d", "N*4e", "N*4f", "N*4g", "N*4h", "N*5d", "N*5e", "N*5f", "N*5g", "N*5h", "N*5i", "N*6c", "N*6d", "N*6e", "N*6f", "N*6g", "N*6h", "N*7c", "N*7d", "N*7e", "N*7f", "N*7g", "N*7h", "N*7i", "N*8c", "N*8d", "N*8e", "N*8f", "N*8g", "N*8h", "N*9c", "N*9d", "N*9e", "N*9f", "N*9g", "N*9h", "N*9i", "P*1c", "P*1d", "P*1e", "P*1f", "P*1g", "P*1h", "P*1i", "P*2c", "P*2d", "P*2e", "P*2f", "P*2g", "P*2h", "P*2i", "P*3c", "P*3d", "P*3e", "P*3f", "P*3g", "P*3h", "P*3i", "P*4b", "P*4c", "P*4d", "P*4e", "P*4f", "P*4g", "P*4h", "P*5d", "P*5e", "P*5f", "P*5g", "P*5h", "P*5i", "P*6b", "P*6c", "P*6d", "P*6e", "P*6f", "P*6g", "P*6h", "P*7c", "P*7d", "P*7e", "P*7f", "P*7g", "P*7h", "P*7i", "P*8b", "P*8c", "P*8d", "P*8e", "P*8f", "P*8g", "P*8h", "P*9b", "P*9c", "P*9d", "P*9e", "P*9f", "P*9g", "P*9h", "P*9i", "R*1a", "R*1c", "R*1d", "R*1e", "R*1f", "R*1g", "R*1h", "R*1i", "R*2a", "R*2c", "R*2d", "R*2e", "R*2f", "R*2g", "R*2h", "R*2i", "R*3a", "R*3c", "R*3d", "R*3e", "R*3f", "R*3g", "R*3h", "R*3i", "R*4a", "R*4b", "R*4c", "R*4d", "R*4e", "R*4f", "R*4g", "R*4h", "R*5a", "R*5d", "R*5e", "R*5f", "R*5g", "R*5h", "R*5i", "R*6a", "R*6b", "R*6c", "R*6d", "R*6e", "R*6f", "R*6g", "R*6h", "R*7a", "R*7c", "R*7d", "R*7e", "R*7f", "R*7g", "R*7h", "R*7i", "R*8a", "R*8b", "R*8c", "R*8d", "R*8e", "R*8f", "R*8g", "R*8h", "R*9b", "R*9c", "R*9d", "R*9e", "R*9f", "R*9g", "R*9h", "R*9i", "S*1a", "S*1c", "S*1d", "S*1e", "S*1f", "S*1g", "S*1h", "S*1i", "S*2a", "S*2c", "S*2d", "S*2e", "S*2f", "S*2g", "S*2h", "S*2i", "S*3a", "S*3c", "S*3d", "S*3e", "S*3f", "S*3g", "S*3h", "S*3i", "S*4a", "S*4b", "S*4c", "S*4d", "S*4e", "S*4f", "S*4g", "S*4h", "S*5a", "S*5d", "S*5e", "S*5f", "S*5g", "S*5h", "S*5i", "S*6a", "S*6b", "S*6c", "S*6d", "S*6e", "S*6f", "S*6g", "S*6h", "S*7a", "S*7c", "S*7d", "S*7e", "S*7f", "S*7g", "S*7h", "S*7i", "S*8a", "S*8b", "S*8c", "S*8d", "S*8e", "S*8f", "S*8g", "S*8h", "S*9b", "S*9c", "S*9d", "S*9e", "S*9f", "S*9g", "S*9h", "S*9i", "4i4b", "5c1g", "5c2f", "5c3a", "5c3e", "5c4b", "5c4d", "5c6b", "5c6d", "5c7a", "5c7e", "5c8f", "5c9g", "6i6b", "8i8b", "9a1a", "9a2a", "9a3a", "9a4a", "9a5a", "9a6a", "9a7a", "9a8a", "9a9b", "9a9c", "9a9d", "9a9e", "9a9f", "9a9g", "9a9h", "9a9i" } };
    }

    [DataTestMethod]
    [DynamicData(nameof(GenerateMovesTestcases), DynamicDataSourceType.Method)]
    public void GenerateMovesTest(string sfen, string[] expected)
    {
        var pos = new Position { Sfen = sfen };

        pos.GenerateMoves()
            .Select(x => x.ToUsi())
            .Should()
            .BeEquivalentTo(expected);
    }

    [DataTestMethod]
    // 玉は逃げられないし取ることもできない
    [DataRow("3lkl3/9/9/b8/9/r8/4K4/r8/9 w B2G2S2N2L9P2g2s2n9p 1", Square.S56, true)]
    // 桂馬で取れる
    [DataRow("3lkl3/9/9/b8/8b/r8/4K4/r4N3/9 w 2G2SN2L9P2g2s2n9p 1", Square.S56, false)]
    // 銀で取れる
    [DataRow("3lkl3/9/9/b8/8b/r8/4KS3/r8/9 w 2GS2N2L9P2g2s2n9p 1", Square.S56, false)]
    // 金で取れる
    [DataRow("3lkl3/9/9/b8/8b/r8/3GK4/r8/9 w G2S2N2L9P2g2s2n9p 1", Square.S56, false)]
    // 角で取れる
    [DataRow("3lkl3/9/9/b8/9/r8/4K4/r8/1B7 w 2G2S2N2L9P2g2s2n9p 1", Square.S56, false)]
    // 飛車で取れる
    [DataRow("3lkl3/9/9/b8/9/r7R/4K4/3PPP3/9 w B2G2S2N2L6P2g2s2n9p 1", Square.S56, false)]
    // 馬で取れる
    [DataRow("3lkl3/9/9/b8/4+B4/r8/4K4/r8/9 w 2G2S2N2L9P2g2s2n9p 1", Square.S56, false)]
    // 竜で取れる
    [DataRow("3lkl3/9/9/b8/9/r8/4K+R3/3PPP3/9 w B2G2S2N2L6P2g2s2n9p 1", Square.S56, false)]
    // ピンされている駒では取れない(角によるピン)
    [DataRow("3lkl3/9/9/b8/6b2/r4G3/4KP3/r8/9 w G2S2N2L8P2g2s2n9p 1", Square.S56, true)]
    // ピンされている駒では取れない(飛車によるピン)
    [DataRow("3lkl3/9/9/b8/9/r8/r2BK4/3PP4/9 w 2G2S2N2L7P2g2s2n9p 1", Square.S56, true)]
    // 現在はピンされているが歩を打つことによってピンが解消されるケース
    [DataRow("3lkl3/4l4/9/b8/4R4/r8/4K4/3PP4/9 w B2G2S2NL7P2g2s2n9p 1", Square.S56, false)]
    // 歩を打つことによって利きが遮られて逃げられるようになるケース１
    [DataRow("3lkl3/9/9/b8/5N3/r8/4KP3/3PPS3/9 w RB2GSNL6P2g2s2nl9p 1", Square.S56, false)]
    // 歩を打つことによって利きが遮られて逃げられるようになるケース２
    [DataRow("8k/9/9/9/3r5/1K7/3+r5/2b6/9 w B2G2S2N3L9P2g2s2nl9p 1", Square.S85, false)]
    // 自駒が邪魔で逃げられないケース
    [DataRow("3lkl3/9/9/b8/4g4/3N1P3/r2SKG2r/3PBS3/9 w NL7P2g2s2nl9p 1", Square.S56, true)]
    public void IsUchifuzumeTest(string sfen, Square to, bool expected)
    {
        var pos = new Position { Sfen = sfen };
        var actual = pos.IsUchifuzume(to);

        Assert.AreEqual(expected, actual, pos.ToString());
    }

    static IEnumerable<object[]> IsSuicideMoveTestcases()
    {
        yield return new object[] { "9/9/9/9/4g4/9/1r3GK2/7S1/k7+b b Rb2g3s4n4l18p 1", MoveExtensions.MakeDrop(Piece.Rook, Square.S91), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S66), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S36), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S58), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S47), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S19), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S59, Square.S58), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S26), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r2g1K2/9/k3P1R1+b b BS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S27), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S17), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S44), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k4BR1+b b GP2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S49, Square.S27), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r3GK2/7S1/k7+b b Rb2g3s4n4l18p 1", MoveExtensions.MakeDrop(Piece.Rook, Square.S91), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S66), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S36), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S58), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S47), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S19), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S59, Square.S58), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S26), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r2g1K2/9/k3P1R1+b b BS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S27), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S17), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S44), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k4BR1+b b GP2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S49, Square.S27), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r3GK2/7S1/k7+b b Rb2g3s4n4l18p 1", MoveExtensions.MakeDrop(Piece.Rook, Square.S91), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S66), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S36), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r4K2/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S58), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S57, Square.S47), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S19), false };
        yield return new object[] { "9/9/9/9/4g4/9/1r2G1K2/7S1/k3P1R1+b b B2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S59, Square.S58), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S46), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S26), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), true };
        yield return new object[] { "9/9/9/9/7g1/9/1r2g1K2/9/k3P1R1+b b BS2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S39, Square.S19), false };
        yield return new object[] { "9/9/9/9/7g1/9/1r4K2/9/k3P1R1+b b BGS2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S77), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S27), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Gold, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S47), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S37, Square.S48), false };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S17), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S28, Square.S27), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k3P1R1+b b BG2g3s4n4l17p 1", MoveExtensions.MakeDrop(Piece.Bishop, Square.S44), true };
        yield return new object[] { "9/9/9/9/7g1/9/6K1r/7S1/k4BR1+b b GP2g3s4n4l17p 1", MoveExtensions.MakeMove(Square.S49, Square.S27), false };
    }

    [DataTestMethod]
    [DynamicData(nameof(IsSuicideMoveTestcases), DynamicDataSourceType.Method)]
    public void IsSuicideMoveTest(string sfen, Move move, bool expected)
    {
        var pos = new Position { Sfen = sfen };
        var actual = !pos.IsLegal(move);

        Assert.AreEqual(expected, actual, $"{pos}\nmove:{move.ToUsi()}");
    }
}