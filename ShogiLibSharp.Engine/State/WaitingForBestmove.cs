using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class WaitingForBestmove : StateBase
    {
        protected TaskCompletionSource<(Move, Move)> tcs;
        public WaitingForBestmove(TaskCompletionSource<(Move, Move)> tcs)
        {
            this.tcs = tcs;
        }

        public override string Name => "bestmove 待ち";

        public override void Bestmove(UsiEngine context,string message)
        {
            context.State = new PlayingGame();
            Misc.NotifyBestmoveReceived(tcs, message);
        }
    }
}
