using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    /// <summary>
    /// 持ち時間 <br/>
    /// 加算ルールの場合は、加算前の持ち時間を表すため <br/>
    /// 使用可能な時間はこれに秒読みと加算を加えたものとなる
    /// </summary>
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
