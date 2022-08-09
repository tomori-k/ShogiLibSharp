using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    public record ConnectOptions
    {
        public string HostName { get; init; } = "localhost";
        public int Port { get; init; } = 4081;
        public string UserName { get; init; } = "";
        public string Password { get; init; } = "";
        public bool SendPv { get; init; } = false;
    }
}
