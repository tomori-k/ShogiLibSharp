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

        public override void SetOption(IEngineProcess process, UsiEngine context)
        {
            throw new NotImplementedException();
        }

        public override void IsReady(IEngineProcess process, TaskCompletionSource tcs, UsiEngine context)
        {
            context.State = new AwaitingReadyOk(tcs);
            process.SendLine("isready");
        }

        public override void Quit(IEngineProcess process, TaskCompletionSource tcs, UsiEngine context)
        {
            context.State = new Quiting(tcs);
            process.SendLine("quit");
        }
    }
}
