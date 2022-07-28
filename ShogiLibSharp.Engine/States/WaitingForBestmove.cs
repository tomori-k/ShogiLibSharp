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
            SetBestmove(tcs, message);
        }

        public override void StopWaitingForBestmove(UsiEngine context)
        {
            context.State = new PlayingGame();
            tcs.TrySetException(new EngineException("タイムアウト時間を超えても bestmove が返ってきませんでした。"));
        }

        public override void Dispose(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new ObjectDisposedException(nameof(context), "UsiEngine が Dispose されました。"));
        }
    }
}
