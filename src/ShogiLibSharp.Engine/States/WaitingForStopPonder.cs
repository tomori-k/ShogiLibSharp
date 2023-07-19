using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForStopPonder : StateBase
    {
        public override string Name => "ponder の停止待ち";

        bool stopped = false;
        Position pos;
        SearchLimit limits;
        TaskCompletionSource<SearchResult> tcs;

        public WaitingForStopPonder(Position pos, SearchLimit limits, TaskCompletionSource<SearchResult> tcs)
        {
            this.pos = pos;
            this.limits = limits;
            this.tcs = tcs;
        }

        public override void Bestmove(UsiEngine context, string message)
        {
            if (stopped)
            {
                context.State = new PlayingGame();
                tcs.TrySetResult(new SearchResult(Move.None, Move.None, new()));
            }
            else
            {
                context.State = new WaitingForBestmoveOrStop(tcs);
                context.SendGo(pos.SfenWithMoves(), limits);
            }
        }

        public override void StopGo(UsiEngine context)
        {
            stopped = true;
        }

        public override void Dispose(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new ObjectDisposedException(nameof(context), "UsiEngine が Dispose されました。"));
        }

        public override void Exited(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new EngineException("プロセスが予期せず終了しました。"));
        }
    }
}
