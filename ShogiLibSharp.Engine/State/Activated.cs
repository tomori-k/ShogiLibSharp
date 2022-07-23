using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class Activated : StateBase
    {
        public override string Name => "プロセス起動中";

        public override void SetOption(Process process, UsiEngine context)
        {
            throw new NotImplementedException();
        }

        public override void IsReady(Process process, TaskCompletionSource tcs, UsiEngine context)
        {
            process.StandardInput.WriteLine("isready");
            context.State = new AwaitingReadyOk(tcs);
        }

        public override void Quit(Process process, TaskCompletionSource tcs, UsiEngine context)
        {
            process.StandardInput.WriteLine("quit");
            context.State = new Quiting(tcs);
        }
    }
}
