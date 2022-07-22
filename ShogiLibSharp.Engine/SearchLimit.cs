using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    public class SearchLimit
    {
        public int Btime { get; set; }
        public int Wtime { get; set; }
        public int Binc { get; set; }
        public int Winc { get; set; }
        public int Byoyomi { get; set; }
    }
}
