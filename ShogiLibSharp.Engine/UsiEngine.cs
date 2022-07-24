using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.State;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    public class UsiEngine
    {
        private IEngineProcess process;
        private object stateSyncObj = new();
        internal StateBase State { get; set; } = new Deactivated();

        public UsiEngine(string fileName, string arguments = "")
        {
            var si = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            this.process = new EngineProcess()
            {
                StartInfo = si,
                EnableRaisingEvents = true,
            };
            this.process.StdOutReceived += Process_StdOutReceived;
            this.process.StdErrReceived += Process_StdErrReceived;
            this.process.Exited += Process_Exited;
        }

        /// <summary>
        /// テスト用
        /// </summary>
        /// <param name="process"></param>
        public UsiEngine(IEngineProcess process)
        {
            this.process = process;
            this.process.StdOutReceived += Process_StdOutReceived;
            this.process.StdErrReceived += Process_StdErrReceived;
            this.process.Exited += Process_Exited;
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
                    State.Bestmove(process, message, this);
                }
            }
        }

        private void Process_StdErrReceived(string? message)
        {
            throw new NotImplementedException();
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            lock (stateSyncObj)
            {
                State.Exited(this);
            }
        }

        public async Task BeginAsync()
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.Begin(process, tcs, this);
            }
            await tcs.Task;
        }

        public void SetOption()
        {
            lock (stateSyncObj)
            {
                State.SetOption(process, this);
            }
        }

        public async Task IsReadyAsync()
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.IsReady(process, tcs, this);
            }
            await tcs.Task;
        }

        public void StartNewGame()
        {
            lock (stateSyncObj)
            {
                State.StartNewGame(process, this);
            }
        }

        public async Task QuitAsync()
        {
            var tcs = new TaskCompletionSource();
            lock (stateSyncObj)
            {
                State.Quit(process, tcs, this);
            }
            await tcs.Task;
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (stateSyncObj)
            {
                State.Go(process, pos, limits, tcs, this);
            }
            var registration = ct.Register(() =>
            {
                lock (stateSyncObj)
                {
                    State.Cancel(process, this);
                }
            });
            using (registration) // この Go に対するキャンセルを解除
            {
                return await tcs.Task;
            }
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (stateSyncObj)
            {
                State.GoPonder(process, pos, limits, this);
            }
        }

        public async Task<(Move, Move)> StopPonderAsync()
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (stateSyncObj)
            {
                State.StopPonder(process, tcs, this);
            }
            return await tcs.Task;
        }

        public void Gameover(string message)
        {
            lock (stateSyncObj)
            {
                State.Gameover(process, message, this);
            }
        }
    }
}