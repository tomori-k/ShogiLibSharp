using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForBestmove : StateBase
    {
        private TaskCompletionSource<(Move, Move)> tcs;
        public WaitingForBestmove(TaskCompletionSource<(Move, Move)> tcs)
        {
            this.tcs = tcs;
        }

        public override string Name => "bestmove 待ち";

        public override void Bestmove(UsiEngine context,string message)
        {
            context.State = new PlayingGame();
            UsiCommand.NotifyBestmoveReceived(tcs, message);
        }

        public sealed override void StopWaitingForBestmove(UsiEngine context)
        {
            context.State = new PlayingGame();
            tcs.SetException(new EngineException("タイムアウト時間を超えても bestmove が返ってきませんでした。"));
        }
    }
}
