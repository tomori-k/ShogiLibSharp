using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public record TimeRule
    {
        public TimeSpan TimeUnit { get; init; } = TimeSpan.FromSeconds(1.0);
        public TimeSpan LeastTimePerMove { get; init; }
        public TimeSpan TotalTime { get; init; }
        public TimeSpan Byoyomi { get; init; }
        public TimeSpan Delay { get; init; }
        public TimeSpan Increment { get; init; }
        public bool IsRoundUp { get; init; }
    }
}
