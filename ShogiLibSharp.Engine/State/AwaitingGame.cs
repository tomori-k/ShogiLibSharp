﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Engine.State
{
    internal class AwaitingGame : StateBase
    {
        public override string Name => "usinewgame 待ち";

        public override void StartNewGame(Process process, UsiEngine context)
        {
            process.StandardInput.WriteLine("usinewgame");
            context.State = new PlayingGame();
        }
    }
}
