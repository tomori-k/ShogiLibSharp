using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.State
{
    internal class PlayingGame : StateBase
    {
        public override string Name => "go コマンド待機";

        public override void Go(Process process, Position pos, SearchLimit limits, ref StateBase currentState)
        {
            currentState = new AwaitingBestmoveOrStop();
            process.StandardInput.SendGo(pos.SfenWithMoves(), limits);
        }

        public override void GoPonder(Process process, Position pos, SearchLimit limits, ref StateBase currentState)
        {
            var sfen = pos.SfenWithMoves();
            currentState = new Pondering(sfen);
            process.StandardInput.SendGo(sfen, limits, ponder: true);
        }

        public override void Gameover(Process process, string message, ref StateBase currentState)
        {
            process.StandardInput.WriteLine($"gameover {message}");
            currentState = new Activated();
        }
    }
}
