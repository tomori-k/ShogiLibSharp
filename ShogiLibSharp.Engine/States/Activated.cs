﻿using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.States
{
    internal class Activated : StateBase
    {
        public override string Name => "プロセス起動中";

        public override void SetOption(UsiEngine context)
        {
            throw new NotImplementedException();
        }

        public override void IsReady(UsiEngine context, TaskCompletionSource tcs)
        {
            context.State = new WaitingForReadyOk(tcs);
            foreach (var (name, value) in context.Options)
            {
                context.Send($"setoption name {name} value {value.Value}");
            }
            context.Send("isready");
        }

        public override void Quit(UsiEngine context, TaskCompletionSource tcs)
        {
            context.State = new Quiting(tcs);
            context.Send("quit");
        }
    }
}
