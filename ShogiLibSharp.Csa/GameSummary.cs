using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public record GameSummary
    {
        public string GameId { get; init; }
        public string BlackName { get; init; }
        public string WhiteName { get; init; }
        public Color Color { get; init; }
        public Color StartColor { get; init; }
        public int? MaxMoves { get; init; }
        public TimeRule TimeRule { get; init; }
        public Position StartPos { get; init; }
        public List<(Move, TimeSpan)> Moves { get; init; }
    }
}
