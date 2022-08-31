using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Exceptions;
using ShogiLibSharp.Engine.Options;
using ShogiLibSharp.Engine.Process;
using ShogiLibSharp.Engine.States;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;

// https://www.nuits.jp/entry/net-standard-internals-visible-to
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ShogiLibSharp.Engine.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]   // Moq

namespace ShogiLibSharp.Engine
{
    /// <summary>
    /// USI プロトコル対応エンジンを扱うクラス
    /// </summary>
    public class UsiEngine : IDisposable
    {
        static readonly Regex IdCommandRegex = new(@"^id\s+(name|author)\s+(.+)$", RegexOptions.Compiled);

        IEngineProcess process;
        object syncObj = new();

        /*
         * IEngineProcess の SendLine の呼び出しの内部で StdOutReceived が
         * Invoke されると lock がネストする可能性があるので、メッセージの受信は
         * 必ず非同期に処理
         * 例:
         * lock (syncObj)
         * SendLine("isready")
         *  StdOutReceived("readyok") // これを Task.Run().Wait() などで囲ったりすると
         *    lock (syncObj)          // ここでデッドロック（スレッドが変わるため）
         *      State.ReadyOk();
         */

        Channel<string> stdoutChannel = Channel
            .CreateUnbounded<string>(new UnboundedChannelOptions
            { SingleWriter = true, SingleReader = true, AllowSynchronousContinuations = false });
        Task stdoutTask;

        internal StateBase State { get; set; } = new Deactivated();
        internal ILogger<UsiEngine> Logger { get; }

        public string Name { get; private set; } = "";
        public string Author { get; private set; } = "";
        public Dictionary<string, IUsiOptionValue> Options { get; } = new();

        public TimeSpan UsiOkTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan ReadyOkTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan BestmoveResponseTimeout { get; set; } = TimeSpan.FromSeconds(10.0);
        public TimeSpan ExitWaitingTime { get; set; } = TimeSpan.FromSeconds(10.0);

        public event Action<UsiInfo>? Info;

        public UsiEngine(string fileName, string workingDir, ILogger<UsiEngine> logger, string arguments = "")
        {
            this.stdoutTask = ReceiveStdoutAsync();
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

        public UsiEngine(string fileName, ILogger<UsiEngine> logger, string arguments = "")
            : this(fileName, Path.GetDirectoryName(fileName) ?? "", logger, arguments)
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
        internal UsiEngine(IEngineProcess process)
        {
            this.stdoutTask = ReceiveStdoutAsync();
            this.process = process;
            this.Logger = NullLogger<UsiEngine>.Instance;
            SetEventCallback();
        }

        void SetEventCallback()
        {
            this.process.StdOutReceived += Process_StdOutReceived;
            this.process.StdErrReceived += s => Logger.LogWarning($"stderr: {s}");
            this.process.Exited += Process_Exited;
        }

        async Task ReceiveStdoutAsync()
        {
            while (await stdoutChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                var message = await stdoutChannel.Reader.ReadAsync().ConfigureAwait(false);
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
                    var match = IdCommandRegex.Match(message);
                    if (match.Success)
                    {
                        if (match.Groups[1].Value == "name") Name = match.Groups[2].Value;
                        else Author = match.Groups[2].Value;
                    }
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
                else if (message.StartsWith("info"))
                {
                    lock (syncObj)
                    {
                        State.Info(this, message);
                    }

                    try
                    {
                        this.Info?.Invoke(UsiCommand.ParseInfo(message));
                    }
                    catch (FormatException e)
                    {
                        Logger.LogWarning(e, "info コマンドの解釈に失敗しました");
                    }
                }
            }
        }

        void Process_StdOutReceived(string? message)
        {
            if (message is null) return;
            Logger.LogTrace($"  > {message}");
            stdoutChannel.Writer.WriteAsync(message).AsTask().Wait();
        }

        void Process_Exited(object? sender, EventArgs e)
        {
            lock (syncObj)
            {
                State.Exited(this);
            }
            Logger.LogInformation($"{Name}({Author}) exited.");
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
                Logger.LogTrace($"< {command}");
                process.SendLine(command);
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

        async Task BeginAsyncImpl(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (syncObj)
            {
                State.Begin(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (syncObj)
                {
                    if (!tcs.Task.IsCompleted)
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

        async Task IsReadyAsyncImpl(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (syncObj)
            {
                State.IsReady(this, tcs);
            }
            using var registration = ct.Register(() =>
            {
                lock (syncObj)
                {
                    if (!tcs.Task.IsCompleted)
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
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (syncObj)
            {
                State.Quit(this, tcs);
            }
            using var cts = new CancellationTokenSource(ExitWaitingTime);
            using var registration = cts.Token.Register(() =>
            {
                lock (syncObj)
                {
                    if (tcs.Task.IsCompleted) return;
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
        public async Task<SearchResult> GoAsync(Position pos, SearchLimit limits, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (syncObj)
            {
                State.Go(this, pos, limits, tcs);
            }

            using var cts = new CancellationTokenSource();
            using var registration = ct.Register(async () =>
            {
                lock (syncObj)
                {
                    if (tcs.Task.IsCompleted) return;
                    State.StopGo(this);
                }
                try
                {
                    await Task.Delay(BestmoveResponseTimeout, cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                lock (syncObj)
                {
                    if (tcs.Task.IsCompleted) return;
                    State.StopWaitingForBestmove(this);
                }
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
        public async Task<SearchResult> StopPonderAsync()
        {
            var tcs = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (syncObj)
            {
                State.StopPonder(this, tcs);
            }

            await Task.WhenAny(tcs.Task, Task.Delay(BestmoveResponseTimeout))
                .ConfigureAwait(false);

            lock (syncObj)
            {
                if (!tcs.Task.IsCompleted)
                    State.StopWaitingForBestmove(this);
            }

            return await tcs.Task.ConfigureAwait(false);
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
                stdoutChannel.Writer.Complete();
                this.process.Dispose();
            }
        }
    }
}