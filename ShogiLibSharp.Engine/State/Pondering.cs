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

        public override void NotifyPonderHit(IEngineProcess process, UsiEngine context)
        {
            context.State = new PonderHit(ponderingPos);
            process.SendLine("ponderhit");
        }

        public override void Stop(IEngineProcess process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            context.State = new AwaitingBestmove(tcs);
            process.SendLine("stop");
        }

        public override void Bestmove(string message, UsiEngine context)
        {
            context.State = new PonderFinished(ponderingPos, message);
        }
    }
}
