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

        public override void NotifyPonderHit(Process process, UsiEngine context)
        {
            process.StandardInput.WriteLine("ponderhit");
            context.State = new PonderHit(ponderingPos);
        }

        public override void Stop(Process process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            process.StandardInput.WriteLine("stop");
            context.State = new AwaitingBestmove(tcs);
        }

        public override void Bestmove(string message, UsiEngine context)
        {
            context.State = new PonderFinished(ponderingPos, message);
        }
    }
}
