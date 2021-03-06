using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
using ShogiLibSharp.Engine.Options;
using ShogiLibSharp.Engine.Process;
using ShogiLibSharp.Engine.States;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    /// <summary>
    /// USI プロトコル対応エンジンを扱うクラス
    /// </summary>
    public class UsiEngine : IDisposable
    {
        private IEngineProcess process;
        private object syncObj = new();
        internal StateBase State { get; set; } = new Deactivated();
        internal ILogger<UsiEngine> Logger { get; }

        public string Name { get; private set; } = "";
        public string Author { get; private set; } = "";
        public Dictionary<string, IUsiOptionValue> Options { get; } = new();

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
            this.Logger = NullLogger<UsiEngine>.Instance;
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
            else if (message.StartsWith("option"))
            {
                try
                {
                    var (name, value) = UsiCommand.ParseOption(message);
                    Options[name] = value;
                }
                catch (FormatException e)
                {
                    Logger.LogWarning(e, "エンジンオプションを解析できません");
                }
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
            var ponderFlag = ponder ? " ponder" : "";
            Send($"position {sfenWithMoves}");
            Send($"go{ponderFlag} {limits}");
        }

        /// <summary>
        /// エンジンにコマンドを送信
        /// </summary>
        /// <param name="command"></param>
        public void Send(string command)
        {
            lock (syncObj)
            {
                this.StdIn?.Invoke(command);
                this.process.SendLine(command);
            }
        }

        /// <summary>
        /// プロセスを起動して usi コマンドを送信し、usiok が返ってくるまで待機
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">CancellationToken により処理がキャンセルされたときにスロー。</exception>
        /// <exception cref="EngineException">エンジンが落ちる、タイムアウト時間を超えても返事がないときなどにスロー。</exception>
        /// <exception cref="ObjectDisposedException">起動中に Dispose() が呼ばれたときにスロー。</exception>
        public async Task BeginAsync(CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(UsiOkTimeout);
            try
            {
                await BeginAsyncImpl(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(e.Message, e, ct);
                }
                else
                {
                    throw new EngineException($"usiok が {UsiOkTimeout.TotalSeconds} 秒待っても返ってきませんでした。");
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
            await tcs.Task.ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// isready コマンドを送信し、readyok が返ってくるまで待機
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">CancellationToken により処理がキャンセルされたときにスロー。</exception>
        /// <exception cref="EngineException">エンジンが落ちる、タイムアウト時間を超えても返事がないときなどにスロー。</exception>
        /// <exception cref="ObjectDisposedException">待機中に Dispose() が呼ばれたときにスロー。</exception>
        public async Task IsReadyAsync(CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ReadyOkTimeout);
            try
            {
                await IsReadyAsyncImpl(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(e.Message, e, ct);
                }
                else
                {
                    throw new EngineException($"readyok が {UsiOkTimeout.TotalSeconds} 秒待っても返ってきませんでした。");
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
            await tcs.Task.ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// usinewgame
        /// </summary>
        public void StartNewGame()
        {
            lock (syncObj)
            {
                State.StartNewGame(this);
            }
        }

        /// <summary>
        /// quit コマンドを送信し、プロセスが終了するまで待機 <br/>
        /// quit 送信後 ExitWaitingTime 経っても終了しない場合は強制的に Kill
        /// </summary>
        /// <exception cref="ObjectDisposedException">終了待機中に Dispose() が呼ばれたときにスロー。</exception>
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
            await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// go コマンドを送信し、bestmove が返ってくるまで待機
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="limits"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">CancellationToken により処理がキャンセルされたときにスロー。</exception>
        /// <exception cref="EngineException">エンジンが落ちる、タイムアウト時間を超えても返事がないときなどにスロー。</exception>
        /// <exception cref="ObjectDisposedException">探索中に Dispose() が呼ばれたときにスロー。</exception>
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

            var result = await tcs.Task.ConfigureAwait(false);
            cts.Cancel();
            return result;
        }

        /// <summary>
        /// go ponder コマンドを送信
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="limits"></param>
        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (syncObj)
            {
                State.GoPonder(this, pos, limits);
            }
        }

        /// <summary>
        /// ponder の停止を行い、bestmove が返ってくるまで待つ
        /// </summary>
        /// <returns></returns>
        public async Task<(Move, Move)> StopPonderAsync()
        {
            using var cts = new CancellationTokenSource();
            var _ = Task.Delay(BestmoveResponseTimeout, cts.Token)
                .ContinueWith(x =>
                {
                    lock (syncObj)
                    {
                        State.StopWaitingForBestmove(this);
                    }
                });
            var tcs = new TaskCompletionSource<(Move, Move)>();
            lock (syncObj)
            {
                State.StopPonder(this, tcs);
            }
            var result = await tcs.Task.ConfigureAwait(false);
            cts.Cancel();
            return result;
        }

        /// <summary>
        /// gameover コマンドを送信
        /// </summary>
        /// <param name="message"></param>
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