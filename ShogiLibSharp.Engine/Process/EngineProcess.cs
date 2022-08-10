using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Process
{
    // IEngineProcess を噛ませたい
    internal class EngineProcess : System.Diagnostics.Process, IEngineProcess
    {
        public event Action<string?>? StdOutReceived;
        public event Action<string?>? StdErrReceived;

        public EngineProcess() : base()
        {
            OutputDataReceived += EngineProcess_OutputDataReceived;
            ErrorDataReceived += EngineProcess_ErrorDataReceived;
        }

        private void EngineProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            StdOutReceived?.Invoke(e.Data);
        }

        private void EngineProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            StdErrReceived?.Invoke(e.Data);
        }

        public void SendLine(string message)
        {
            this.StandardInput.WriteLine(message);
        }
    }
}
