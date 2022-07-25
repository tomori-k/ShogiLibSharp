using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForBestmoveOrStop : WaitingForBestmove
    {
        public WaitingForBestmoveOrStop(TaskCompletionSource<(Move, Move)> tcs) : base(tcs)
        {
        }

        public override string Name => "bestmove または stop 待ち";

        public override void Cancel(UsiEngine context)
        {
            context.State = new WaitingForBestmove(this.tcs);
            context.Send("stop");
        }
    }
}
