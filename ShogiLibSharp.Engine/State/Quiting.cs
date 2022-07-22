using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class Quiting : StateBase
    {
        public override string Name => "終了処理中";
        public TaskCompletionSource Tcs { get; } = new();

        public override void Exited(UsiEngine context)
        {
            context.SetStateWithLock(new Invalid());
            Tcs.SetResult();
        }
    }
}
