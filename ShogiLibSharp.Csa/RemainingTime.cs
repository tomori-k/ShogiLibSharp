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
        private TimeSpan[] time = new TimeSpan[2];

        public TimeSpan this[Color c]
        {
            get => time[(int)c];
            set => time[(int)c] = value;
        }

        public RemainingTime() { }

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
