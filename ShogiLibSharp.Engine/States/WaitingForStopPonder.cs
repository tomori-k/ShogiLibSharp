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

        Position pos;
        SearchLimit limits;
        TaskCompletionSource<(Move, Move)> tcs;

        public WaitingForStopPonder(Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs)
        {
            this.pos = pos;
            this.limits = limits;
            this.tcs = tcs;
        }

        public override void Bestmove(UsiEngine context,string message)
        {
            context.State = new WaitingForBestmoveOrStop(tcs);
            context.SendGo(pos.SfenWithMoves(), limits);
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
