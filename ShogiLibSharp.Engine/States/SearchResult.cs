using System;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.States
{
    public record SearchResult(Move Bestmove, Move Ponder, List<UsiInfo> InfoList);
}