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

        string ponderedPos;
        string bestmoveCmd;
        List<UsiInfo> infoList;

        public PonderFinished(
            string ponderedPos, string bestmoveCmd, List<UsiInfo> infoList)
        {
            this.ponderedPos = ponderedPos;
            this.bestmoveCmd = bestmoveCmd;
            this.infoList = infoList;
        }

        public override void Go(
            UsiEngine context, Position pos, SearchLimit limits, TaskCompletionSource<SearchResult> tcs)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderedPos)
            {
                context.State = new PlayingGame();
                SetBestmove(tcs, bestmoveCmd, infoList);
            }
            else
            {
                context.State = new WaitingForBestmoveOrStop(tcs);
                context.SendGo(sfen, limits);
            }
        }

        public override void StopPonder(UsiEngine context, TaskCompletionSource<SearchResult> tcs)
        {
            context.State = new PlayingGame();
            SetBestmove(tcs, bestmoveCmd, infoList);
        }
    }
}
