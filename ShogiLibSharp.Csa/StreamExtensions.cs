using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    internal static class StreamExtensions
    {
        public static async Task WriteLineLFAsync(this Stream stream, string s, CancellationToken ct)
        {
            var bytes = Encoding.ASCII.GetBytes(s + '\n');
            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);
        }
    }
}
