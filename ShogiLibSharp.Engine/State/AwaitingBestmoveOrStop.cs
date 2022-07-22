using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class AwaitingBestmoveOrStop : AwaitingBestmove
    {
        public override string Name => "bestmove または stop 待ち";

        public override void Stop(Process process, UsiEngine context)
        {
            context.SetStateWithLock(new AwaitingBestmove());
            process.StandardInput.WriteLine("stop");
        }
    }
}
