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
        public TaskCompletionSource Tcs { get; }

        public AwaitingReadyOk()
        {
            this.Tcs = new TaskCompletionSource();
        }

        public override void ReadyOk(UsiEngine context)
        {
            context.SetStateWithLock(new AwaitingGame());
            this.Tcs.SetResult();
        }
    }
}
