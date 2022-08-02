using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public enum EndGameState
    {
        None,
        Sennichite,
        OuteSennichite,
        IllegalMove,
        TimeUp,
        Resign,
        Jishogi,
        Chudan,
        IllegalAction,
    }
}
