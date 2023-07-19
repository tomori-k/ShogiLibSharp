using System;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine
{
    public record UsiInfo
    {
        public int? Depth { get; set; } = null;
        public int? SelDepth { get; set; } = null;
        public TimeSpan? Time { get; set; } = null;
        public ulong? Nodes { get; set; } = null;
        public int? MultiPv { get; set; } = null;
        public Score? Score { get; set; } = null;
        public Move? CurrMove { get; set; } = null;
        public int? Hashfull { get; set; } = null;
        public ulong? Nps { get; set; } = null;
        public string? String { get; set; } = null;
        public List<Move> Pv { get; set; } = new();
    }

    public record Score(int Value, bool IsMate, Bound Bound);

    public enum Bound
    {
        Exact, UpperBound, LowerBound,
    }
}

