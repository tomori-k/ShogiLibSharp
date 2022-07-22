using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class PonderFinished : BestmoveAwaitable
    {
        public override string Name => "次の go が来るまでに ponder が終了";

        private string ponderedPos, bestmoveCmd;

        public PonderFinished(string ponderedPos, string bestmoveCmd)
        {
            (this.ponderedPos, this.bestmoveCmd) = (ponderedPos, bestmoveCmd);
        }

        public override void Go(Process process, Position pos, SearchLimit limits, ref StateBase currentState)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderedPos)
            {
                currentState = new PlayingGame();
                try
                {
                    var (bestmove, ponder) = Misc.ParseBestmove(bestmoveCmd);
                    this.Tcs.SetResult((bestmove, ponder));
                }
                catch (FormatException e)
                {
                    this.Tcs.SetException(e);
                }
            }
            else
            {
                currentState = new AwaitingBestmoveOrStop();
                process.StandardInput.SendGo(sfen, limits);
            }
        }

        public override void Stop(Process process, ref StateBase currentState)
        {
            currentState = new PlayingGame();
            try
            {
                var (bestmove, ponder) = Misc.ParseBestmove(bestmoveCmd);
                this.Tcs.SetResult((bestmove, ponder));
            }
            catch (FormatException e)
            {
                this.Tcs.SetException(e);
            }
        }
    }
}
