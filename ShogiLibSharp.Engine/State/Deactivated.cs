using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class Deactivated : StateBase
    {
        public override string Name => "プロセス未起動";

        public override void Begin(IEngineProcess process, TaskCompletionSource tcs, UsiEngine context)
        {
            context.State = new WaitingForUsiOk(tcs);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.SendLine("usi");
        }
    }
}
