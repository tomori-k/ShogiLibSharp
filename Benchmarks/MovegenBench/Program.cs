// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShogiLibSharp.Core;

BenchmarkRunner.Run<Bench>();

[IterationCount(3)]
public class Bench
{
    static readonly (string, int, ulong)[] testcases = new[]
    {
        (Position.Hirate, 5, 19861490UL),
        ("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1", 4, 516925165UL),
        ("R8/2K1S1SSk/4B4/9/9/9/9/9/1L1L1L3 b RBGSNLP3g3n17p 1", 3, 53393368UL),
    };

    [Benchmark]
    public void PerftNoUnsafeBlock()
    {
        
        foreach (var (sfen, depth, expected) in testcases)
        {
            var pos = new Position(sfen);
            var nodes = PerftImpl(pos, depth);
            if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
        }
    }

    [Benchmark]
    public unsafe void PerftUseUnsafe()
    {
        foreach (var (sfen, depth, expected) in testcases)
        {
            var pos = new Position(sfen);
            var nodes = PerftUnsafeImpl(pos, depth);
            if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
        }
    }

    private static ulong PerftImpl(Position pos, int depth)
    {
        var moves = Movegen.GenerateMoves(pos);

        if (depth == 1)
            return (ulong)moves.Count;

        ulong count = 0;

        foreach (Move m in moves)
        {
            pos.DoMove_PseudoLegal(m);
            count += PerftImpl(pos, depth - 1);
            pos.UndoMove();
        }

        return count;
    }

    private static unsafe ulong PerftUnsafeImpl(Position pos, int depth)
    {
        var moves = stackalloc Move[600];
        var end = Movegen.GenerateMovesUnsafe(moves, pos);

        if (depth == 1)
            return (ulong)(end - moves);

        ulong count = 0;
        var p = moves;

        while (p != end)
        { 
            pos.DoMove_PseudoLegal(*p++);
            count += PerftUnsafeImpl(pos, depth - 1);
            pos.UndoMove();
        }

        return count;
    }
}