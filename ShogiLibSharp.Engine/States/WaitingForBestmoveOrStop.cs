using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
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
        public override string Name => "bestmove または stop 待ち";

        List<UsiInfo> infoList;
        TaskCompletionSource<SearchResult> tcs;

        public WaitingForBestmoveOrStop(TaskCompletionSource<SearchResult> tcs)
        {
            this.infoList = new();
            this.tcs = tcs;
        }

        public WaitingForBestmoveOrStop(TaskCompletionSource<SearchResult> tcs, List<UsiInfo> infoList)
        {
            this.tcs = tcs;
            this.infoList = infoList;
        }

        public override void Bestmove(UsiEngine context, string message)
        {
            context.State = new PlayingGame();
            SetBestmove(tcs, message, infoList);
        }

        public override void Info(UsiEngine context, string message)
        {
            if (UsiCommand.TryParseInfo(message, out var info)) infoList.Add(info);
        }

        public override void StopGo(UsiEngine context)
        {
            context.State = new WaitingForBestmove(this.tcs, infoList);
            context.Send("stop");
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
