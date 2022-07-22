using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal class AwaitingBestmove : BestmoveAwaitable
    {
        public override string Name => "bestmove 待ち";

        public override void Bestmove(string message, ref StateBase currentState)
        {
            currentState = new PlayingGame();
            try
            {
                var (move, ponder) = Misc.ParseBestmove(message);
                this.Tcs.SetResult((move, ponder));
            }
            catch (FormatException e)
            {
                this.Tcs.SetException(e);
            } 
        }
    }
}
