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
        public override string Name => "bestmove 待ち";

        List<UsiInfo> infoList;
        TaskCompletionSource<SearchResult> tcs;

        public WaitingForBestmove(TaskCompletionSource<SearchResult> tcs, List<UsiInfo> infoList)
        {
            this.infoList = infoList;
            this.tcs = tcs;
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

        public override void Exited(UsiEngine context)
        {
            context.State = new Invalid();
            tcs.TrySetException(new EngineException("プロセスが予期せず終了しました。"));
        }
    }
}
