using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    public class SearchLimit
    {
        public TimeSpan Btime { get; set; }
        public TimeSpan Wtime { get; set; }
        public TimeSpan Binc { get; set; }
        public TimeSpan Winc { get; set; }
        public TimeSpan Byoyomi { get; set; }

        public static SearchLimit Create(TimeSpan btime, TimeSpan wtime, TimeSpan byoyomi)
        {
            return new SearchLimit
            {
                Btime = btime,
                Wtime = wtime,
                Binc = TimeSpan.Zero,
                Winc = TimeSpan.Zero,
                Byoyomi = byoyomi,
            };
        }

        public static SearchLimit Create(TimeSpan btime, TimeSpan wtime, TimeSpan binc, TimeSpan winc)
        {
            return new SearchLimit
            {
                Btime = btime,
                Wtime = wtime,
                Binc = binc,
                Winc = winc,
                Byoyomi = TimeSpan.Zero,
            };
        }

        public static SearchLimit Create(TimeSpan byoyomi)
        {
            return new SearchLimit
            {
                Btime = TimeSpan.Zero,
                Wtime = TimeSpan.Zero,
                Binc = TimeSpan.Zero,
                Winc = TimeSpan.Zero,
                Byoyomi = byoyomi,
            };
        }

        public override string ToString()
        {
            return Binc == TimeSpan.Zero && Winc == TimeSpan.Zero
                ? $"btime {(long)Btime.TotalMilliseconds} wtime {(long)Wtime.TotalMilliseconds} byoyomi {(long)Byoyomi.TotalMilliseconds}"
                : $"btime {(long)Btime.TotalMilliseconds} wtime {(long)Wtime.TotalMilliseconds} binc {(long)Binc.TotalMilliseconds} winc {(long)Winc.TotalMilliseconds}";
        }
    }
}
