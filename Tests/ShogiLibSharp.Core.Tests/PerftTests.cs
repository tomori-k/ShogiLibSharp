using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass]
public class PerftTests
{
    [DataTestMethod]
    [DataRow(Position.Hirate, 5, 19861490UL)]
    [DataRow(Position.Matsuri, 4, 516925165UL)]
    [DataRow(Position.MaxLegalMoves, 3, 53393368UL)]
    public void PerftTest(string sfen, int depth, ulong expected)
    {
        var pos = new Position(sfen);
        var nodes = Perft(pos, depth);

        Assert.AreEqual(expected, nodes);
    }

    static ulong Perft(Position pos, int depth)
    {
        var moves = pos.GenerateMoves();

        if (depth == 1)
            return (ulong)moves.Count;

        ulong count = 0;

        foreach (Move m in moves)
        {
            pos.DoMoveUnsafe(m);
            count += Perft(pos, depth - 1);
            pos.TryUndoMove();
        }

        return count;
    }
}