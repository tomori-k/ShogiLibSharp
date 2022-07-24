using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.State
{
    internal class Pondering : StateBase
    {
        public override string Name => "ponder 中";

        private string ponderingPos;

        public Pondering(string ponderingPos)
        {
            this.ponderingPos = ponderingPos;
        }

        public override void Go(
            IEngineProcess process, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderingPos)
            {
                context.State = new AwaitingBestmoveOrStop(tcs);
                process.SendLine("ponderhit");
            }
            else
            {
                context.State = new WaitingForStopPonder(pos, limits, tcs);
                process.SendLine("stop");
            }
        }

        public override void StopPonder(IEngineProcess process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            context.State = new AwaitingBestmove(tcs);
            process.SendLine("stop");
        }

        public override void Bestmove(IEngineProcess process, string message, UsiEngine context)
        {
            context.State = new PonderFinished(ponderingPos, message);
        }
    }
}
