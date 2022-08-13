using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://www.nuits.jp/entry/net-standard-internals-visible-to
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ShogiLibSharp.Kifu")]
[assembly: InternalsVisibleTo("ShogiLibSharp.Csa")]

namespace ShogiLibSharp.Core
{
    internal class PeekableReader : IDisposable
    {
        TextReader reader;
        string? nextLine = null;

        public PeekableReader(TextReader reader)
        {
            this.reader = reader;
        }

        public string? PeekLine()
        {
            if (nextLine is null)
            {
                nextLine = reader.ReadLine();
            }
            return nextLine;
        }

        public string? ReadLine()
        {
            if (nextLine is null)
            {
                return reader.ReadLine();
            }
            else
            {
                var tmp = nextLine;
                nextLine = null;
                return tmp;
            }
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
