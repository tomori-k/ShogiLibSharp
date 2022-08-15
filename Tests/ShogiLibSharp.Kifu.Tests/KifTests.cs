using System.Diagnostics;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu.Tests;

[TestClass]
public class KifTests
{
    [TestMethod, Timeout(1000)]
    public void ParseTest()
    {
        var kif1 = @"";

        using var sr = new StringReader(kif1);
        var kifu = Kif.Parse(sr);
        Trace.WriteLine(kifu.GameInfo.Names[0]);
        Trace.WriteLine(kifu.GameInfo.Names[1]);
        foreach (var m in kifu.MoveLists[0].Moves)
        {
            Trace.WriteLine($"{m.Move.Usi()} {m.Elapsed} {m.Comment}");
        }
    }
}
