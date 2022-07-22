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

        public override void Go(Process process, Position pos, SearchLimit limits, ref StateBase currentState)
        {
            var sfen = pos.SfenWithMoves();
            if (sfen == ponderingPos)
            {
                currentState = new AwaitingBestmoveOrStop();
            }
            else
                throw new InvalidOperationException("思考中に、別の局面の思考を開始することはできません。");
        }

        public override void Stop(Process process, ref StateBase currentState)
        {
            currentState = new AwaitingBestmove();
            process.StandardInput.WriteLine("stop");
        }

        public override void Bestmove(string message, ref StateBase currentState)
        {
            currentState = new PonderFinished(ponderingPos, message);
        }
    }
}
