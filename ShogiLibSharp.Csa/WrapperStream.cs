using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    // .NET 7 から StreamReader の ReadLineAsync が キャンセルできるらしい（corefx のソースコードを見る限り）

    /// <summary>
    /// CsaClient で送受信に使う NetworkStream をラップするクラス
    /// ASCII 限定で ReadLineAsync と WriteLineLFAsync のキャンセルが可能
    /// </summary>
    internal class WrapperStream : IDisposable
    {
        private const int BufferSize = 1024;

        private Stream stream;
        private byte[] buffer = new byte[BufferSize];
        private char[] charBuffer = new char[BufferSize];
        private int charPos = 0;
        private int charLen = 0;
        private bool disposed = false;

        public WrapperStream(Stream stream)
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

        /// <summary>
        /// Stream から1行読み取る <br/>
        /// Stream のデータは ASCII と仮定
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Stream に1行書き込む <br/>
        /// 改行文字はLFを使用する<br/>
        /// 書き込む文字列は ASCII に変換できると仮定
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task WriteLineLFAsync(string s, CancellationToken ct)
        {
            var bytes = Encoding.ASCII.GetBytes(s + '\n');
            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);
        }
    }
}
