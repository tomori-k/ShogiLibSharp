using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass]
public class PositionTests
{
    [TestMethod]
    public void SfenMovesTest()
    {
        var pos = new Position(Position.Hirate);

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
        var pos = new Position(sfen);
        var actual = pos.CanDeclareWin();

        Assert.AreEqual(expected, actual, pos.ToString());
    }

    [TestMethod()]
    [DeploymentItem(@"kifu/sennichite/1.csa")]
    [DeploymentItem(@"kifu/sennichite/2.csa")]
    [DeploymentItem(@"kifu/not_sennichite/1.csa")]
    [DeploymentItem(@"kifu/not_sennichite/2.csa")]
    public void CheckRepetitionTest()
    {
        //CheckRepetitionTest1();
        //CheckRepetitionTest2(new[] {
        //    @"kifu/sennichite/1.csa",
        //    @"kifu/sennichite/2.csa",
        //}, true);
        //CheckRepetitionTest2(new[] {
        //    @"kifu/not_sennichite/1.csa",
        //    @"kifu/not_sennichite/2.csa",
        //}, false);
    }

    static IEnumerable<object[]> RepetitionTestcases()
    {
        yield return new object[] { "5k3/9/9/4B4/9/9/9/9/8K b RB2G2S2N3L6Pr2g2s2nl12p 1", new[] { "5d6c", "4a3b", "6c5d", "3b4a" }, Repetition.Lose };
        yield return new object[] { "9/9/1k4+R2/9/9/9/9/9/8K w RB2G2S2N3L6Pb2g2s2nl12p 1", new[] { "8c8d", "3c3d", "8d8c", "3d3c" }, Repetition.Win };
        yield return new object[] { "9/8r/9/9/7K1/9/9/9/k8 w RB3G2S2N3L7Pbg2snl11p 1", new[] { "1b2b", "R*2d", "2b2d", "2e2d", "R*1b", "2d2e" }, Repetition.Draw };
        yield return new object[] { "9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b2b", "2e3e", "2b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Win };
        yield return new object[] { "9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b4b", "2e3e", "4b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Draw };
    }

    [DataTestMethod]
    [DynamicData(nameof(RepetitionTestcases), DynamicDataSourceType.Method)]
    public void RepetitionTest(string sfen, string[] cycle, Repetition expected)
    {
        var pos = new Position(sfen);

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

    //private void CheckRepetitionTest2(string[] fileNames, bool repeated)
    //{
    //    foreach (var fileName in fileNames)
    //    {
    //        var kifu = Kifu.Csa.Parse(fileName);
    //        var pos = new Position(kifu.StartPos);
    //        foreach (var mi in kifu.MoveLists[0].Moves)
    //        {
    //            Assert.AreEqual(Repetition.None, pos.CheckRepetition());
    //            pos.DoMove(mi.Move);
    //        }
    //        Assert.AreEqual(repeated, pos.CheckRepetition() == Repetition.Draw);
    //    }
    //}

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
        var pos = new Position(sfen);
        var pinnedByBlack = pos.PinnedBy(Color.Black);
        var pinnedByWhite = pos.PinnedBy(Color.White);

        Assert.AreEqual(new(byBlackPattern), pinnedByBlack);
        Assert.AreEqual(new(byWhitePattern), pinnedByWhite);
    }
}