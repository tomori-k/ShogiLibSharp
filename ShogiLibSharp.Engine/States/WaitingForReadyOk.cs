using ShogiLibSharp.Engine.Exceptions;
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
            tcs.TrySetResult();
        }

        public override void CancelReadyOk(UsiEngine context)
        {
            context.State = new Activated();
            tcs.TrySetResult();
        }

        public override void Dispose(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new ObjectDisposedException(nameof(context), "UsiEngine が Dispose されました。"));
        }
    }
}
