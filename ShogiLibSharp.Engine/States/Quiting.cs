using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class Quiting : StateBase
    {
        public override string Name => "終了処理中";
        private TaskCompletionSource tcs;

        public Quiting(TaskCompletionSource tcs)
        {
            this.tcs = tcs;
        }

        public override void Exited(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetResult();
        }

        public override void Dispose(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new ObjectDisposedException(nameof(context), "プロセスの終了を待っている間に UsiEngine が Dispose されました。"));
        }
    }
}
