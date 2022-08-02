using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    internal enum ClientState
    {
        Unconnected,
        WaitingForGame,
        WaitingForAgree,
        PlayingGame,
        Disconnected,
    }
}
