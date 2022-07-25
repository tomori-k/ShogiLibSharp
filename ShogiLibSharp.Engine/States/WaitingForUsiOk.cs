using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class WaitingForUsiOk : StateBase
    {
        public override string Name => "usiok 待ち";
        private TaskCompletionSource tcs;

        public WaitingForUsiOk(TaskCompletionSource tcs)
        {
            this.tcs = tcs;
        }

        public override void UsiOk(UsiEngine context)
        {
            context.State = new Activated();
            tcs.SetResult();
        }
    }
}
