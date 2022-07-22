using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class AwaitingUsiOk : StateBase
    {
        public override string Name => "usiok 待ち";
        public TaskCompletionSource Tcs { get; }

        public AwaitingUsiOk()
        {
            this.Tcs = new TaskCompletionSource();
        }

        public override void UsiOk(UsiEngine context)
        {
            context.SetStateWithLock(new Activated());
            Tcs.SetResult();
        }
    }
}
