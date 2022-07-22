using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    public class EngineException : Exception
    {
        public EngineException()
        {
        }

        public EngineException(string message)
            : base(message)
        {
        }

        public EngineException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
