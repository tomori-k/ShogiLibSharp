using System;
namespace ShogiLibSharp.Csa
{
    /// <summary>
    /// ShogiServerClient の接続オプション
    /// </summary>
    public record ShogiServerOptions : ConnectOptions
    {
        public string GameName { get; init; }

        public ShogiServerOptions(string HostName, string UserName, string Password, string GameName)
            : base(HostName, UserName, Password)
        {
            this.GameName = GameName;
            this.SendPv = true;
        }
    }
}

