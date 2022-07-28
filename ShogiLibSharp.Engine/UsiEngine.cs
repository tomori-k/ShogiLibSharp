using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Process;
using ShogiLibSharp.Engine.States;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    public class UsiEngine : IDisposable
    {
        private IEngineProcess process;
        private object syncObj = new();
        internal StateBase State { get; set; } = new Deactivated();
        internal ILogger<UsiEngine> Logger { get; }

        public string Name { get; private set; } = "";
        public string Author { get; private set; } = "";

        public event Action<string>? StdIn;
        public event Action<string?>? StdOut;
        public event Action<string?>? StdErr;

        public TimeSpan UsiOkTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan ReadyOkTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan BestmoveResponseTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan ExitWaitingTime { get; set; } = TimeSpan.FromSeconds(10.0);

        public UsiEngine(string fileName, string workingDir, ILogger<UsiEngine> logger, string arguments = "")
        {
            this.Logger = logger;
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

        public UsiEngine(string fileName, string workingDir, string arguments = "")
            : this(fileName, workingDir, NullLogger<UsiEngine>.Instance, arguments)
        {
        }

        public UsiEngine(string fileName, string arguments = "")
            : this(fileName, Path.GetDirectoryName(fileName) ?? "", NullLogger<UsiEngine>.Instance, arguments)
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
            this.process.StdOutReceived += s => StdOut?.Invoke(s);
            this.process.StdErrReceived += s => StdErr?.Invoke(s);
            this.process.StdOutReceived += Process_StdOutReceived;
            this.process.Exited += Process_Exited;
        }

        private void Process_StdOutReceived(string? message)
        {
            if (message == null) return;

            if (message == "usiok")
            {
                lock (syncObj)
                {
                    State.UsiOk(this);
                }
            }
            else if (message == "readyok")
            {
                lock (syncObj)
                {
                    State.ReadyOk(this);
                }
            }
            else if (message.StartsWith("bestmove"))
            {
                lock (syncObj)
                {
                    State.Bestmove(this, message);
                }
            }
            else if (message.StartsWith("id"))
            {
                var sp = message.Split();
                if (sp.Length < 3) return;
                else if (sp[1] == "name") this.Name = sp[2];
                else if (sp[1] == "author") this.Author = sp[2];
            }
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            lock (syncObj)
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
            lock (syncObj)
            {
                this.StdIn?.Invoke(command);
                this.process.SendLine(command);
            }
        }

        public async Task BeginAsync(CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(UsiOkTimeout);
            try
            {
                await BeginAsyncImpl(cts.Token);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(e.Message, e, ct);
                }
                else
                {
                    throw new TimeoutException($"usiok が {UsiOkTimeout.TotalSeconds} 秒待っても返ってきませんでした。");
                }
            }
        }

        private async Task BeginAsyncImpl(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource();
            lock (syncObj)
            {
                State.Begin(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (syncObj)
                {
                    State.CancelUsiOk(this);
                }
            });
            await tcs.Task;
            ct.ThrowIfCancellationRequested();
        }

        public void SetOption()
        {
            lock (syncObj)
            {
                State.SetOption(this);
            }
        }

        public async Task IsReadyAsync(CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ReadyOkTimeout);
            try
            {
                await IsReadyAsyncImpl(cts.Token);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(e.Message, e, ct);
                }
                else
                {
                    throw new TimeoutException($"readyok が {UsiOkTimeout.TotalSeconds} 秒待っても返ってきませんでした。");
                }
            }
        }

        private async Task IsReadyAsyncImpl(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource();
            lock (syncObj)
            {
                State.IsReady(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (syncObj)
                {
                    State.CancelReadyOk(this);
                }
            });
            await tcs.Task;
            ct.ThrowIfCancellationRequested();
        }

        public void StartNewGame()
        {
            lock (syncObj)
            {
                State.StartNewGame(this);
            }
        }

        public async Task QuitAsync()
        {
            var tcs = new TaskCompletionSource();
            lock (syncObj)
            {
                State.Quit(this, tcs);
            }
            using var cts = new CancellationTokenSource(ExitWaitingTime);
            using var registration = cts.Token.Register(() =>
            {
                lock (syncObj)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                }
            });
            await tcs.Task;
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (syncObj)
            {
                State.Go(this, pos, limits, tcs);
            }

            using var cts = new CancellationTokenSource();
            using var registration = ct.Register(() =>
            {
                var task = Task.Delay(BestmoveResponseTimeout, cts.Token);
                lock (syncObj)
                {
                    State.StopGo(this);
                }
                task.ContinueWith(x =>
                {
                    lock (syncObj)
                    {
                        State.StopWaitingForBestmove(this);
                    }
                });
            }); // この Go に対するキャンセルを解除

            var result = await tcs.Task;
            cts.Cancel();
            return result;
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (syncObj)
            {
                State.GoPonder(this, pos, limits);
            }
        }

        public async Task<(Move, Move)> StopPonderAsync()
        {
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (syncObj)
            {
                State.StopPonder(this, tcs);
            }
            return await tcs.Task;
        }

        public void Gameover(string message)
        {
            lock (syncObj)
            {
                State.Gameover(this, message);
            }
        }

        public void Dispose()
        {
            lock (syncObj)
            {
                State.Dispose(this);
                this.process.Dispose();
            }
        }
    }
}