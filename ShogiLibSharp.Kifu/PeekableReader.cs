using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Kifu
{
    internal class PeekableReader : IDisposable
    {
        TextReader reader;
        string? nextLine = null;
        char commentPrefix;

        public PeekableReader(TextReader reader, char commentPrefix)
        {
            this.reader = reader;
            this.commentPrefix = commentPrefix;
        }

        public string? PeekLine()
        {
            if (nextLine is null)
            {
                nextLine = ReadLineImpl();
            }
            return nextLine;
        }

        public string? ReadLine()
        {
            if (nextLine is null)
            {
                return ReadLineImpl();
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

        string? ReadLineImpl()
        {
            while (true)
            {
                if (reader.ReadLine() is not { } line) return null;
                if (!line.StartsWith(commentPrefix)) return line;
            }
        }
    }
}
