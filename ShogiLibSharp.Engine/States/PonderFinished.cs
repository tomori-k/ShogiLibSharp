using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
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
            UsiEngine context, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderedPos)
            {
                context.State = new PlayingGame();
                SetBestmove(tcs, bestmoveCmd);
            }
            else
            {
                context.State = new WaitingForBestmoveOrStop(tcs);
                context.SendGo(sfen, limits);
            }
        }

        public override void StopPonder(UsiEngine context, TaskCompletionSource<(Move, Move)> tcs)
        {
            context.State = new PlayingGame();
            SetBestmove(tcs, bestmoveCmd);
        }
    }
}
