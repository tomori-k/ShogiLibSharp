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

        public override void CancelUsiOk(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.SetException(new Exceptions.EngineException("タイムアウト時間を超えても、エンジンから usiok が返ってきませんでした。"));
        }
    }
}
