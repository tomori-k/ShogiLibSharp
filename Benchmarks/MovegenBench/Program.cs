// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShogiLibSharp.Core;

BenchmarkRunner.Run<Bench>();
BenchmarkRunner.Run<BenchUnsafe>();


[IterationCount(3)]
public class Bench
{
    [Benchmark]
    public void PerftHirate()
    {
        var (sfen, depth, expected) = (Position.Hirate, 5, 19861490UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    [Benchmark]
    public void PerftMatsuri()
    {
        var (sfen, depth, expected) =  ("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1", 4, 516925165UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    [Benchmark]
    public void PerftMax()
    {
        var (sfen, depth, expected) = ("R8/2K1S1SSk/4B4/9/9/9/9/9/1L1L1L3 b RBGSNLP3g3n17p 1", 3, 53393368UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    static ulong PerftImpl(Position pos, int depth)
    {
        var moves = Movegen.GenerateMoves(pos);

        if (depth == 1)
            return (ulong)moves.Count;

        ulong count = 0;

        foreach (var m in moves)
        {
            pos.DoMove_PseudoLegal(m);
            count += PerftImpl(pos, depth - 1);
            pos.UndoMove();
        }

        return count;
    }
}

[IterationCount(3)]
public class BenchUnsafe
{
    [Benchmark]
    public void PerftHirate()
    {
        var (sfen, depth, expected) = (Position.Hirate, 5, 19861490UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    [Benchmark]
    public void PerftMatsuri()
    {
        var (sfen, depth, expected) = ("l6nl/5+P1gk/2np1S3/p1p4Pp/3P2Sp1/1PPb2P1P/P5GS1/R8/LN4bKL w RGgsn5p 1", 4, 516925165UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    [Benchmark]
    public void PerftMax()
    {
        var (sfen, depth, expected) = ("R8/2K1S1SSk/4B4/9/9/9/9/9/1L1L1L3 b RBGSNLP3g3n17p 1", 3, 53393368UL);
        var pos = new Position(sfen);
        var nodes = PerftImpl(pos, depth);
        if (nodes != expected) throw new Exception($"sfen={sfen},nodes={nodes},expcted={expected}");
    }

    [SkipLocalsInit]
    static unsafe ulong PerftImpl(Position pos, int depth)
    {
        var buffer = stackalloc Move[Movegen.BufferSize];
        var end = Movegen.GenerateMoves(buffer, pos);

        if (depth == 1)
            return (ulong)(end - buffer);

        ulong count = 0;
        var p = buffer;

        while (p != end)
        {
            pos.DoMove_PseudoLegal(*p++);
            count += PerftImpl(pos, depth - 1);
            pos.UndoMove();
        }

        return count;
    }
}