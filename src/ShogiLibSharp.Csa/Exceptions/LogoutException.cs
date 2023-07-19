using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa.Exceptions
{
    public class LogoutException : Exception
    {
        public LogoutException()
        {
        }

        public LogoutException(string message)
            : base(message)
        {
        }

        public LogoutException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
