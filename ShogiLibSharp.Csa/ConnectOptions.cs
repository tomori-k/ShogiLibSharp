using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public record ConnectOptions(string HostName, string UserName, string Password)
    {
        public int Port { get; init; } = 4081;
        public bool SendPv { get; init; } = false;
    }
}
