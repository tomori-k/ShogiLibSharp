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

        public override void Go(
            IEngineProcess process, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            context.State = new AwaitingBestmoveOrStop(tcs);
            Misc.SendGo(process, pos.SfenWithMoves(), limits);
        }

        public override void GoPonder(IEngineProcess process, Position pos, SearchLimit limits, UsiEngine context)
        {
            var sfen = pos.SfenWithMoves();

            context.State = new Pondering(sfen);
            Misc.SendGo(process, sfen, limits, ponder: true);
        }

        public override void Gameover(IEngineProcess process, string message, UsiEngine context)
        {
            context.State = new Activated();
            process.SendLine($"gameover {message}");
        }
    }
}
