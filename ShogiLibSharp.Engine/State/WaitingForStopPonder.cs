using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class WaitingForStopPonder : StateBase
    {
        public override string Name => "ponder の停止待ち";

        private Position pos;
        private SearchLimit limits;
        private TaskCompletionSource<(Move, Move)> tcs;

        public WaitingForStopPonder(Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs)
        {
            this.pos = pos;
            this.limits = limits;
            this.tcs = tcs;
        }

        public override void Bestmove(IEngineProcess process, string message, UsiEngine context)
        {
            context.State = new WaitingForBestmoveOrStop(tcs);
            Misc.SendGo(process, pos.SfenWithMoves(), limits);
        }
    }
}
