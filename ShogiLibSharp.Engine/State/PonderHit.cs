using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class PonderHit : StateBase
    {
        public override string Name => "ponderhit";
        private string ponderingPos;

        public PonderHit(string ponderingPos)
        {
            this.ponderingPos = ponderingPos;
        }

        public override void Go(Process process, Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderingPos)
            {
                context.State = new AwaitingBestmoveOrStop(tcs);
            }
            else
                throw new InvalidOperationException("思考中に、別の局面の思考を開始することはできません。");
        }

        public override void Stop(Process process, TaskCompletionSource<(Move, Move)> tcs, UsiEngine context)
        {
            process.StandardInput.WriteLine("stop");
            context.State = new AwaitingBestmove(tcs);
        }

        public override void Bestmove(string message, UsiEngine context)
        {
            context.State = new PonderFinished(ponderingPos, message);
        }
    }
}
