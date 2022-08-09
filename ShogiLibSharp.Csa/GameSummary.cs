using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public record GameSummary(
        string GameId,
        string BlackName,
        string WhiteName,
        Color Color,
        Color StartColor,
        int? MaxMoves,
        TimeRule TimeRule,
        Position StartPos,
        List<(Move, TimeSpan)> Moves);
}
