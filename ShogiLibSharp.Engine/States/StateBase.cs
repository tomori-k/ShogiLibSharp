using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;

namespace ShogiLibSharp.Engine.States
{
    internal abstract class StateBase
    {
        public abstract string Name { get; }

        public virtual void Begin(UsiEngine context, TaskCompletionSource tcs)
            => throw new InvalidOperationException("既にエンジンを起動しています。");

        public virtual void SetOption(UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name} において、setoption コマンドの送信は不正な操作です。");

        public virtual void IsReady(UsiEngine context, TaskCompletionSource tcs)
            => throw new InvalidOperationException($"状態：{Name} において、isready コマンドの送信は不正な操作です。");

        public virtual void Quit(UsiEngine context, TaskCompletionSource tcs)
            => throw new InvalidOperationException($"状態：{Name} において、quit コマンドの送信は不正な操作です。");

        public virtual void StartNewGame(UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name} において、usinewgame コマンドの送信は不正な操作です。");

        public virtual void Go(
            UsiEngine context, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs)
            => throw new InvalidOperationException($"状態：{Name} において、go コマンドの送信は不正な操作です。");

        public virtual void GoPonder(UsiEngine context, Position pos, SearchLimit limits)
            => throw new InvalidOperationException($"状態：{Name} において、go ponder コマンドの送信は不正な操作です。");

        public virtual void StopGo(UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name} において、探索のキャンセルは不正な操作です。");

        public virtual void StopPonder(UsiEngine context, TaskCompletionSource<(Move, Move)> tcs)
            => throw new InvalidOperationException($"状態：{Name} において、ponder の停止は不正な操作です。");

        public virtual void Gameover(UsiEngine context, string message)
            => throw new InvalidOperationException($"状態：{Name} において、gameover コマンドの送信は不正な操作です。");

        public virtual void UsiOk(UsiEngine context)
            => throw new EngineException($"状態：{Name} において不正なコマンド usiok を受信しました。");

        public virtual void ReadyOk(UsiEngine context)
            => throw new EngineException($"状態：{Name} において不正なコマンド readyok を受信しました。");

        public virtual void Bestmove(UsiEngine context, string message)
            => throw new EngineException($"状態：{Name} において不正なコマンド {message} を受信しました。");

        public virtual void CancelUsiOk(UsiEngine context) { /* 何もしない */ }

        public virtual void CancelReadyOk(UsiEngine context) { /* 何もしない */ }

        public virtual void StopWaitingForBestmove(UsiEngine context) { /* 何もしない */ }

        public virtual void Exited(UsiEngine context)
            => context.State = new Invalid();

        public virtual void Dispose(UsiEngine context)
            => context.State = new Invalid();

        public static void SetBestmove(TaskCompletionSource<(Move, Move)> tcs, string command)
        {
            try
            {
                var (move, ponder) = UsiCommand.ParseBestmove(command);
                tcs.TrySetResult((move, ponder));
            }
            catch (FormatException e)
            {
                tcs.TrySetException(e);
            }
        }
    }
}
