﻿using ShogiLibSharp.Core;
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

        private Position pos;
        private SearchLimit limits;
        private TaskCompletionSource<(Move, Move)> tcs;

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
    }
}