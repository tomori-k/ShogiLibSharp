using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public class RemainingTime
    {
        TimeSpan[] time;

        public TimeSpan this[Color c]
        {
            get => time[(int)c];
            set => time[(int)c] = value;
        }

        public RemainingTime() { this.time = new TimeSpan[2]; }

        public RemainingTime(TimeSpan totalTime)
        {
            this.time = new[] { totalTime, totalTime };
        }

        public RemainingTime(TimeSpan btime, TimeSpan wtime)
        {
            this.time = new[] { btime, wtime };
        }

        public RemainingTime(RemainingTime remTime)
        {
            this.time = (TimeSpan[])remTime.time.Clone();
        }

        public RemainingTime Clone()
        {
            return new RemainingTime(this);
        }
    }
}
