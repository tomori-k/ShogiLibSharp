using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class PonderFinished : StateBase
    {
        public override string Name => "次の go が来るまでに ponder が終了";

        private string ponderedPos, bestmoveCmd;

        public PonderFinished(
            string ponderedPos, string bestmoveCmd)
        {
            (this.ponderedPos, this.bestmoveCmd) = (ponderedPos, bestmoveCmd);
        }

        public override void Go(
            IEngineProcess process, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderedPos)
            {
                context.State = new PlayingGame();
                Misc.NotifyBestmoveReceived(tcs, bestmoveCmd);
            }
            else
            {
                context.State = new AwaitingBestmoveOrStop(tcs);
                Misc.SendGo(process, sfen, limits);
            }
        }

        public override void StopPonder(IEngineProcess process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            context.State = new PlayingGame();
            Misc.NotifyBestmoveReceived(tcs, bestmoveCmd);
        }
    }
}
