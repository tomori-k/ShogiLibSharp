using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.States
{
    internal class WaitingForNextGame : StateBase
    {
        public override string Name => "usinewgame 待ち";

        public override void StartNewGame(UsiEngine context)
        {
            context.State = new PlayingGame();
            context.Send("usinewgame");
        }
    }
}
