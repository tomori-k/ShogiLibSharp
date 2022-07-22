using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.State
{
    internal abstract class BestmoveAwaitable : StateBase
    {
        public TaskCompletionSource<(Move, Move)> Tcs { get; } = new();
    }
}
