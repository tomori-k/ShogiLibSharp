using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Process;
using ShogiLibSharp.Engine.States;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    public class UsiEngine : IDisposable
    {
        private IEngineProcess process;
        private object stateSyncObj = new();
        private object sendLockObj = new();
        internal StateBase State { get; set; } = new Deactivated();

        public event Action<string>? StdIn;
        public event Action<string?>? StdOut;
        public event Action<string?>? StdErr;

        public UsiEngine(string fileName, string workingDir, string arguments = "")
        {
            var si = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir,
            };
            this.process = new EngineProcess()
            {
                StartInfo = si,
                EnableRaisingEvents = true,
            };
            SetEventCallback();
        }

        public UsiEngine(string fileName, string arguments = "")
            : this(fileName, Path.GetDirectoryName(fileName) ?? "", arguments)
        {
        }

        /// <summary>
        /// テスト用
        /// </summary>
        /// <param name="process"></param>
        public UsiEngine(IEngineProcess process)
        {
            this.process = process;
            SetEventCallback();
        }

        private void SetEventCallback()
        {
            this.process.StdOutReceived += Process_StdOutReceived;
            this.process.Exited += Process_Exited;
            this.process.StdOutReceived += s => StdOut?.Invoke(s);
            this.process.StdErrReceived += s => StdErr?.Invoke(s);
        }

        private void Process_StdOutReceived(string? message)
        {
            if (message == null) return;

            if (message == "usiok")
            {
                lock (stateSyncObj)
                {
                    State.UsiOk(this);
                }
            }
            else if (message == "readyok")
            {
                lock (stateSyncObj)
                {
                    State.ReadyOk(this);
                }
            }
            else if (message.StartsWith("bestmove"))
            {
                lock (stateSyncObj)
                {
                    State.Bestmove(this, message);
                }
            }
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            lock (stateSyncObj)
            {
                State.Exited(this);
            }
        }

        internal void BeginProcess()
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        internal void SendGo(string sfenWithMoves, SearchLimit limits, bool ponder = false)
        {
            Send($"position {sfenWithMoves}");

            var ponderFlag = ponder ? " ponder" : "";

            if (limits.Binc == 0 && limits.Winc == 0)
            {
                Send($"go{ponderFlag} btime {limits.Btime} wtime {limits.Wtime} byoyomi {limits.Byoyomi}");
            }
            else
            {
                Send($"go{ponderFlag} btime {limits.Btime} wtime {limits.Wtime} binc {limits.Binc} winc {limits.Winc}");
            }
        }

        public void Send(string command)
        {
            lock (sendLockObj)
            {
                this.process.SendLine(command);
                this.StdIn?.Invoke(command);
            }
        }

        public async Task BeginAsync(CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.Begin(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (stateSyncObj)
                {
                    State.CancelUsiOk(this);
                }
            });
            await tcs.Task;
        }

        public void SetOption()
        {
            lock (stateSyncObj)
            {
                State.SetOption(this);
            }
        }

        public async Task IsReadyAsync(CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.IsReady(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (stateSyncObj)
                {
                    State.CancelReadyOk(this);
                }
            });
            await tcs.Task;
        }

        public void StartNewGame()
        {
            lock (stateSyncObj)
            {
                State.StartNewGame(this);
            }
        }

        public async Task QuitAsync()
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.Quit(this, tcs);
            }
            await tcs.Task;
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (stateSyncObj)
            {
                State.Go(this, pos, limits, tcs);
            }

            using var registration = ct.Register(() =>
            {
                lock (stateSyncObj)
                {
                    State.Cancel(this);
                }
            }); // この Go に対するキャンセルを解除

            return await tcs.Task;
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (stateSyncObj)
            {
                State.GoPonder(this, pos, limits);
            }
        }

        public async Task<(Move, Move)> StopPonderAsync()
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (stateSyncObj)
            {
                State.StopPonder(this, tcs);
            }
            return await tcs.Task;
        }

        public void Gameover(string message)
        {
            lock (stateSyncObj)
            {
                State.Gameover(this, message);
            }
        }

        public void Dispose()
        {
            this.process.Dispose();
        }
    }
}