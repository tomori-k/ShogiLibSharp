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
            context.SetStateWithLock(new PonderHit(ponderingPos));
            process.StandardInput.WriteLine("ponderhit");
        }

        public override void Stop(Process process, UsiEngine context)
        {
            context.SetStateWithLock(new AwaitingBestmove());
            process.StandardInput.WriteLine("stop");
        }

        public override void Bestmove(string message, UsiEngine context)
        {
            context.SetStateWithLock(new PonderFinished(ponderingPos, message));
        }
    }
}
