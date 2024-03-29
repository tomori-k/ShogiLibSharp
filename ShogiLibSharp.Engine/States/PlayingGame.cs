﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.States
{
    internal class PlayingGame : StateBase
    {
        public override string Name => "go コマンド待機";

        public override void Go(
            UsiEngine context, Position pos, SearchLimit limits, TaskCompletionSource<SearchResult> tcs)
        {
            context.State = new WaitingForBestmoveOrStop(tcs);
            context.SendGo(pos.SfenWithMoves(), limits);
        }

        public override void GoPonder(UsiEngine context,Position pos, SearchLimit limits)
        {
            var sfen = pos.SfenWithMoves();

            context.State = new Pondering(sfen);
            context.SendGo(sfen, limits, ponder: true);
        }

        public override void Gameover(UsiEngine context,string message)
        {
            context.State = new Activated();
            context.Send($"gameover {message}");
        }

        public override void Bestmove(UsiEngine context, string message)
        {
            // 無視（たまたま bestmove がタイムアウトより遅れて返ってきたときにエラーが出てほしくない）
        }

        public override void StopPonder(UsiEngine context, TaskCompletionSource<SearchResult> tcs)
        {
            tcs.TrySetResult(new SearchResult(Move.None, Move.None, new()));
        }
    }
}
