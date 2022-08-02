using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    // .NET 7 から StreamReader の ReadLineAsync が キャンセルできるらしい（corefx のソースコードを見る限り）

    internal class AsyncAsciiStreamReader : IDisposable
    {
        private const int BufferSize = 1024;

        private Stream stream;
        private byte[] buffer = new byte[BufferSize];
        private char[] charBuffer = new char[BufferSize];
        private int charPos = 0;
        private int charLen = 0;
        private bool disposed = false;

        public AsyncAsciiStreamReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            charPos = 0;
            charLen = 0;
            stream.Close();
        }

        public async Task<string?> ReadLineAsync(CancellationToken ct)
        {
            if (charPos == charLen)
            {
                if (await ReadBufferAsync(ct) == 0)
                {
                    return null;
                }
            }
            var sb = new StringBuilder();
            do
            {
                int i = charPos;
                while (i < charLen)
                {
                    var ch = charBuffer[i];
                    if (ch == '\r' || ch == '\n')
                    {
                        sb.Append(charBuffer, charPos, i - charPos);
                        charPos = i + 1;
                        if (ch == '\r' && (charPos < charLen || await ReadBufferAsync(ct) > 0))
                        {
                            if (charBuffer[charPos] == '\n')
                            {
                                ++charPos;
                            }
                        }
                        return sb.ToString();
                    }
                    ++i;
                }
                sb.Append(charBuffer, charPos, charLen - charPos);
            } while (await ReadBufferAsync(ct) > 0);
            return sb.ToString();
        }

        private async Task<int> ReadBufferAsync(CancellationToken ct)
        {
            charPos = 0;
            var byteLen = await stream.ReadAsync(buffer, 0, BufferSize, ct);
            charLen = Encoding.ASCII.GetChars(buffer, 0, byteLen, charBuffer, 0);
            return charLen;
        }
    }
}
