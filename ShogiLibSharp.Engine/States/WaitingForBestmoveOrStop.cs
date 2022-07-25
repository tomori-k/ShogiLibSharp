using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForBestmoveOrStop : StateBase
    {
        private TaskCompletionSource<(Move, Move)> tcs;
        public WaitingForBestmoveOrStop(TaskCompletionSource<(Move, Move)> tcs)
        {
            this.tcs = tcs;
        }

        public override string Name => "bestmove または stop 待ち";

        public override void Bestmove(UsiEngine context, string message)
        {
            context.State = new PlayingGame();
            SetBestmove(tcs, message);
        }

        public override void StopGo(UsiEngine context)
        {
            context.State = new WaitingForBestmove(this.tcs);
            context.Send("stop");
        }
    }
}
