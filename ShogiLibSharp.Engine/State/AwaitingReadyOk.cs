using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class AwaitingReadyOk : StateBase
    {
        public override string Name => "readyok 待ち";
        private TaskCompletionSource tcs;

        public AwaitingReadyOk(TaskCompletionSource tcs)
        {
            this.tcs = tcs;
        }

        public override void ReadyOk(UsiEngine context)
        {
            context.State = new AwaitingGame();
            this.tcs.SetResult();
        }
    }
}
