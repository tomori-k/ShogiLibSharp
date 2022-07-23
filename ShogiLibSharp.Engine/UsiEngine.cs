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
                    State.Bestmove(message, this);
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
                if (State is not Deactivated)
                {
                    throw new InvalidOperationException("すでにエンジンを起動しています。");
                }

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // send usi
                process.SendLine("usi");

                State = new AwaitingUsiOk(tcs);
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

        public void IsReady(TaskCompletionSource tcs)
        {
            lock (stateSyncObj)
            {
                State.IsReady(process, tcs, this);
            }
        }

        public async Task IsReadyAsync()
        {
            var tcs = new TaskCompletionSource();
            IsReady(tcs);
            await tcs.Task;
        }

        public void StartNewGame()
        {
            lock (stateSyncObj)
            {
                State.StartNewGame(process, this);
            }
        }

        public void Quit(TaskCompletionSource tcs)
        {
            lock (stateSyncObj)
            {
                State.Quit(process, tcs, this);
            }
        }

        public async Task QuitAsync()
        {
            var tcs = new TaskCompletionSource();
            Quit(tcs);
            await tcs.Task;
        }

        public void Go(Position pos, SearchLimit limits, TaskCompletionSource<(Move, Move)> tcs)
        {
            lock (stateSyncObj)
            {
                State.Go(process, pos, limits, tcs, this);
            }
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits)
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            Go(pos, limits, tcs);
            return await tcs.Task;
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (stateSyncObj)
            {
                State.GoPonder(process, pos, limits, this);
            }
        }

        public void NotifyPonderHit()
        {
            lock (stateSyncObj)
            {
                State.NotifyPonderHit(process, this);
            }
        }

        public void Stop(TaskCompletionSource<(Move, Move)> tcs)
        {
            lock (stateSyncObj)
            {
                State.Stop(process, tcs, this);
            }
        }

        public async Task<(Move, Move)> StopAsync()
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            Stop(tcs);
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