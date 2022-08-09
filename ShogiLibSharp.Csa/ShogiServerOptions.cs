using System;
namespace ShogiLibSharp.Csa
{
    public record ShogiServerOptions : ConnectOptions
    {
        public string GameName { get; init; } = "floodgate-300-10F";

        public ShogiServerOptions()
        {
            SendPv = true;
        }
    }
}

