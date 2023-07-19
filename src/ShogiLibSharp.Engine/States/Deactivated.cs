using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class Deactivated : StateBase
    {
        public override string Name => "プロセス未起動";

        public override void Begin(UsiEngine context,TaskCompletionSource tcs)
        {
            context.State = new WaitingForUsiOk(tcs);
            context.BeginProcess();
            context.Send("usi");
        }
    }
}
