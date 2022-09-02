using System;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine
{
    public record SearchResult(Move Bestmove, Move Ponder, List<UsiInfo> InfoList);
}