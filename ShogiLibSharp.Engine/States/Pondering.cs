﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.States
{
    internal class Pondering : StateBase
    {
        public override string Name => "ponder 中";

        string ponderingPos;
        List<UsiInfo> infoList = new();

        public Pondering(string ponderingPos)
        {
            this.ponderingPos = ponderingPos;
        }

        public override void Go(
            UsiEngine context, Position pos, SearchLimit limits, TaskCompletionSource<SearchResult> tcs)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderingPos)
            {
                context.State = new WaitingForBestmoveOrStop(tcs, infoList);
                context.Send("ponderhit");
            }
            else
            {
                context.State = new WaitingForStopPonder(pos, limits, tcs);
                context.Send("stop");
            }
        }

        public override void StopPonder(UsiEngine context, TaskCompletionSource<SearchResult> tcs)
        {
            context.State = new WaitingForBestmove(tcs, infoList);
            context.Send("stop");
        }

        public override void Bestmove(UsiEngine context, string message)
        {
            context.State = new PonderFinished(ponderingPos, message, infoList);
        }

        public override void Info(UsiEngine context, string message)
        {
            if (UsiCommand.TryParseInfo(message, out var info)) infoList.Add(info);
        }
    }
}
