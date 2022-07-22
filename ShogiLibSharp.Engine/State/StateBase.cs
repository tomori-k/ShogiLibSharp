﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.State
{
    internal abstract class StateBase
    {
        public abstract string Name { get; }

        public virtual void SetOption(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、setoption コマンドの送信は不正な操作です。");

        public virtual void IsReady(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、isready コマンドの送信は不正な操作です。");

        public virtual void Quit(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、quit コマンドの送信は不正な操作です。");

        public virtual void StartNewGame(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、usinewgame コマンドの送信は不正な操作です。");

        public virtual void Go(Process process, Position pos, SearchLimit limits, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、go コマンドの送信は不正な操作です。");

        public virtual void GoPonder(Process process, Position pos, SearchLimit limits, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、go ponder コマンドの送信は不正な操作です。");

        public virtual void NotifyPonderHit(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、ponderhit コマンドの送信は不正な操作です。");

        public virtual void Stop(Process process, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、stop コマンドの送信は不正な操作です。");

        public virtual void Gameover(Process process, string message, UsiEngine context)
            => throw new InvalidOperationException($"状態：{Name}において、gameover コマンドの送信は不正な操作です。");

        public virtual void UsiOk(UsiEngine context)
            => throw new EngineException($"状態：{Name}において不正なコマンド usiok を受信しました。");

        public virtual void ReadyOk(UsiEngine context)
            => throw new EngineException($"状態：{Name}において不正なコマンド readyok を受信しました。");

        public virtual void Bestmove(string message, UsiEngine context)
            => throw new EngineException($"状態：{Name}において不正なコマンド {message} を受信しました。");

        public virtual void Exited(UsiEngine context)
            => context.SetStateWithLock(new Invalid());
    }
}
