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
        public AwaitingBestmoveOrStop(TaskCompletionSource<(Move, Move)> tcs) : base(tcs)
        {
        }

        public override string Name => "bestmove または stop 待ち";

        public override void Stop(Process process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            process.StandardInput.WriteLine("stop");
            context.State = new AwaitingBestmove(tcs);
        }
    }
}
