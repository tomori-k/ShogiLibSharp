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
                throw new InvalidOperationException("違う局面の思考を開始する前に、stop する必要があります。");
        }

        public override void Stop(IEngineProcess process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            context.State = new PlayingGame();
            Misc.NotifyBestmoveReceived(tcs, bestmoveCmd);
        }

        public override void NotifyPonderHit(IEngineProcess process, UsiEngine context)
        {
            // go ponder に対し ponderhit or stop を送る前に、エンジンが
            // bestmove を返してきた状態で、その後に ponderhit を送ろうとしたケース
            // 単純に指示を無視する
        }
    }
}
