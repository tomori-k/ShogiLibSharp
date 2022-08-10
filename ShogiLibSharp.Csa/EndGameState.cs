using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    /// <summary>
    /// 終局状況
    /// </summary>
    public enum EndGameState
    {
        None,
        Sennichite,
        OuteSennichite,
        IllegalMove,
        TimeUp,
        Resign,
        Jishogi,
        MaxMoves,
        Chudan,
        IllegalAction,
    }
}
