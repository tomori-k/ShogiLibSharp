using System.Diagnostics;
using System.Text;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu.Tests;

[TestClass]
public class KifTests
{
    [TestMethod]
    [DeploymentItem("kif/1.kifu")]
    public void ParseTest1()
    {
        var kifu = Kif.Parse("kif/1.kifu", Encoding.UTF8);
        var info = kifu.GameInfo;

        Assert.AreEqual("てすと1", info.Names[0]);
        Assert.AreEqual("テスト１", info.Names[1]);
        Assert.AreEqual(new DateTime(year: 1999, month: 7, day: 15, hour:19, minute: 7, second:12), info.StartTime);
        Assert.AreEqual(new DateTime(year: 2022, month: 8, day: 16, hour:0, minute: 46, second:59), info.EndTime);
        Assert.AreEqual("EVENT１", info.Event);
        Assert.AreEqual("マリアナ海溝", info.Site);

        var moves0 = kifu.MoveLists[0].Moves;

        Assert.AreEqual(Usi.ParseMove("2g2f"), moves0[0].Move);
        Assert.AreEqual("コメント！コメント！コメント！"
            + Environment.NewLine
            + "２行目！"
            + Environment.NewLine, moves0[0].Comment);
        Assert.AreEqual(Usi.ParseMove("7a7b"), moves0[9].Move);
        Assert.AreEqual(new TimeSpan(1, 30, 5), moves0[9].Elapsed);

        var moves1 = kifu.MoveLists[1].Moves;

        Assert.AreEqual(4, kifu.MoveLists[1].StartPly);
        Assert.AreEqual(Usi.ParseMove("9i9h"), moves1[1].Move);
        Assert.AreEqual("悪手 -99999999" + Environment.NewLine, moves1[1].Comment);
    }

    [TestMethod]
    [DeploymentItem("kif/2.kifu")]
    [DeploymentItem("kif/2_usi.txt")]
    public void ParseTest2()
    {
        var kifu = Kif.Parse("kif/2.kifu", Encoding.UTF8);
        var info = kifu.GameInfo;

        Assert.AreEqual(new DateTime(year: 2022, month: 8, day: 15, hour: 14, minute: 7, second: 0), info.StartTime);
        Assert.AreEqual(new DateTime(year: 2022, month: 8, day: 15, hour: 14, minute: 49, second: 0), info.EndTime);
        Assert.AreEqual("その他の棋戦", info.Event);
        Assert.AreEqual("東京都品川区「大崎ブライトコアホール」", info.Site);
        Assert.AreEqual("戸辺　誠 七段", info.Names[0]);
        Assert.AreEqual("羽生善治 九段", info.Names[1]);
        Assert.AreEqual("相振り飛車", info.Opening);

        var movesExpected = File.ReadAllLines("kif/2_usi.txt")
            .Select(x => Usi.ParseMove(x));
        var movesActual = kifu.MoveLists[0].Moves.Select(x => x.Move);

        foreach(var (ex, ac) in movesExpected.Zip(movesActual))
        {
            Assert.AreEqual(ex, ac);
        }
    }

    [TestMethod]
    [DeploymentItem("csa/12.csa")]
    public void ParseCsaAsKifTest()
    {
        Assert.ThrowsException<FormatException>(() => Kif.Parse("csa/12.csa", Encoding.UTF8));
    }

    [TestMethod]
    [DeploymentItem("kif/min.kifu")]
    public void ParseMinimumKifTest()
    {
        var kifu = Kif.Parse("kif/min.kifu", Encoding.UTF8);
        Assert.AreEqual(1, kifu.MoveLists.Count);
        Assert.AreEqual(0, kifu.MoveLists[0].Moves.Count);
    }

    [TestMethod]
    [DeploymentItem("kif/3.kifu")]
    [DeploymentItem("kif/4.kifu")]

    public void IllegalMoveKifTest()
    {
        var kifu = Kif.Parse("kif/3.kifu", Encoding.UTF8);

        Assert.AreEqual("3gkg3/9/9/9/9/9/9/9/8K b R2GS2Nr2b3s2n4l18p 1", new Position(kifu.StartPos).Sfen());
        Assert.AreEqual(Usi.ParseMove("P*5d"), kifu.MoveLists[0].Moves[0].Move);

        Assert.ThrowsException<FormatException>(() => Kif.Parse("kif/4.kifu", Encoding.UTF8));
    }
}
