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

        public override void Go(Process process, Position pos, SearchLimit limits, UsiEngine context)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderingPos)
            {
                context.SetStateWithLock(new AwaitingBestmoveOrStop());
            }
            else
                throw new InvalidOperationException("思考中に、別の局面の思考を開始することはできません。");
        }

        public override void Stop(Process process, UsiEngine context)
        {
            context.SetStateWithLock(new AwaitingBestmove());
            process.StandardInput.WriteLine("stop");
        }

        public override void Bestmove(string message, UsiEngine context)
        {
            context.SetStateWithLock(new PonderFinished(ponderingPos, message));
        }
    }
}
