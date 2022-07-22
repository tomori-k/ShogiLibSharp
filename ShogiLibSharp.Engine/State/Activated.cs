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

        public override void SetOption(Process process, ref StateBase currentState)
        {
            throw new NotImplementedException();
        }

        public override void IsReady(Process process, ref StateBase currentState)
        {
            currentState = new AwaitingReadyOk();
            process.StandardInput.WriteLine("isready");
        }

        public override void Quit(Process process, ref StateBase currentState)
        {
            currentState = new Quiting();
            process.StandardInput.WriteLine("quit");
        }
    }
}
