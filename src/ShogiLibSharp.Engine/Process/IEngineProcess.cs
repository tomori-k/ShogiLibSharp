using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Process
{
    internal interface IEngineProcess : IDisposable
    {
        bool HasExited { get; }

        event Action<string?>? StdOutReceived;
        event Action<string?>? StdErrReceived;
        event EventHandler Exited;
        bool Start();
        void BeginOutputReadLine();
        void BeginErrorReadLine();
        void SendLine(string message);
        void Kill();
        Task WaitForExitAsync(CancellationToken ct = default);
    }
}
