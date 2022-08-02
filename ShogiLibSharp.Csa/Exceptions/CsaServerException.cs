using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa.Exceptions
{
    public class CsaServerException : Exception
    {
        public CsaServerException()
        {
        }

        public CsaServerException(string message)
            : base(message)
        {
        }

        public CsaServerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
