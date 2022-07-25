using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForReadyOk : StateBase
    {
        public override string Name => "readyok 待ち";
        private TaskCompletionSource tcs;

        public WaitingForReadyOk(TaskCompletionSource tcs)
        {
            this.tcs = tcs;
        }

        public override void ReadyOk(UsiEngine context)
        {
            context.State = new WaitingForNextGame();
            this.tcs.SetResult();
        }
    }
}
