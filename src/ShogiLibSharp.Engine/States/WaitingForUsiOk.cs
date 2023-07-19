using ShogiLibSharp.Engine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForUsiOk : StateBase
    {
        public override string Name => "usiok 待ち";
        TaskCompletionSource tcs;

        public WaitingForUsiOk(TaskCompletionSource tcs)
        {
            this.tcs = tcs;
        }

        public override void UsiOk(UsiEngine context)
        {
            context.State = new Activated();
            tcs.TrySetResult();
        }

        public override void CancelUsiOk(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetResult();
        }

        public override void Dispose(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new ObjectDisposedException(nameof(context), "UsiEngine が Dispose されました。"));
        }

        public override void Exited(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new EngineException("プロセスが予期せず終了しました。"));
        }
    }
}
