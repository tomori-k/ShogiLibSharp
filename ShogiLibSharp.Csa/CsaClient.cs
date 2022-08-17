using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ShogiLibSharp.Core;
using ShogiLibSharp.Csa.Exceptions;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

// https://www.nuits.jp/entry/net-standard-internals-visible-to
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ShogiLibSharp.Csa.Tests")]

namespace ShogiLibSharp.Csa
{
    /// <summary>
    /// CSA サーバプロトコル 1.2.1 準拠の対局クライアント
    /// </summary>
    public class CsaClient
    {
        private protected ILogger<CsaClient> logger;
        private protected ReaderWriterWrapper? rw = null;
        private protected IPlayerFactory playerFactory;
        private protected ConnectOptions options;
        private protected bool isWaitingForNextGame = false;
        private protected SemaphoreSlim stateSem = new(1, 1);
        private protected readonly TimeSpan keepAliveInterval;
        
        /// <summary>
        /// 接続タスク <br/>
        /// 通信終了を待ちたいときはこれを await
        /// </summary>
        public Task ConnectionTask { get; }

        public CsaClient(IPlayerFactory playerFactory, ConnectOptions options, CancellationToken ct = default)
            : this(playerFactory, options, TimeSpan.FromSeconds(30.0), NullLogger<CsaClient>.Instance, ct)
        {
        }

        public CsaClient(IPlayerFactory playerFactory, ConnectOptions options, TimeSpan keepAliveInterval, CancellationToken ct = default)
            : this(playerFactory, options, keepAliveInterval, NullLogger<CsaClient>.Instance, ct)
        {
        }

        public CsaClient(IPlayerFactory playerFactory, ConnectOptions options, ILogger<CsaClient> logger, CancellationToken ct = default)
            : this(playerFactory, options, TimeSpan.FromSeconds(30.0), logger, ct)
        {
        }

        public CsaClient(
            IPlayerFactory playerFactory,
            ConnectOptions options,
            TimeSpan keepAliveInterval,
            ILogger<CsaClient> logger,
            CancellationToken ct = default)
        {
            this.playerFactory = playerFactory;
            this.options = options;
            this.keepAliveInterval = keepAliveInterval;
            this.logger = logger;
            ValidateOptions();
            this.ConnectionTask = ConnectAsync(ct);
        }

        private protected virtual void ValidateOptions()
        {
            var invalidName = options.UserName.Length == 0
                || options.UserName.Length > 32
                || options.UserName.Any(c =>
                    !(('0' <= c && c <= '9')
                    || ('a' <= c && c <= 'z')
                    || ('A' <= c && c <= 'Z')
                    || c == '_' || c == '-'));

            if (invalidName)
            {
                throw new ArgumentException(
                    $"ユーザ名は32文字以下で、英数字、ハイフン、アンダースコアのいずれかのみ使用可能です。: {options.UserName}");
            }

            var invalidPassword = options.Password.Length == 0
                || options.Password.Length > 32
                || options.Password.Any(c => char.IsWhiteSpace(c) || !char.IsAscii(c));

            if (invalidPassword)
            {
                throw new ArgumentException(
                    $"パスワードは32文字以下で、非ASCII文字、空白文字は使用できません。: {options.Password}");
            }
        }

        /// <summary>
        /// サーバからログアウト
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task LogoutAsync(CancellationToken ct = default)
        {
            if (ConnectionTask.IsCompleted) return;

            await stateSem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (rw is null || !isWaitingForNextGame) return;
                await rw.WriteLineAsync("LOGOUT", ct).ConfigureAwait(false);
            }
            finally
            {
                stateSem.Release();
            }

            await ConnectionTask.ConfigureAwait(false);
        }

        async Task ConnectAsync(CancellationToken ct)
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(options.HostName, options.Port, ct).ConfigureAwait(false);
            rw = new ReaderWriterWrapper(new CancellableReaderWriter(tcp.GetStream()), logger);
            try
            {
                await CommunicateWithServerAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                rw.Dispose();
            }
        }

        private protected virtual async Task CommunicateWithServerAsync(CancellationToken ct)
        {
            await LoginAsync(ct).ConfigureAwait(false);

            while (true)
            {
                if (!playerFactory.ContinueLogin())
                {
                    Debug.Assert(!isWaitingForNextGame);
                    // ログアウト
                    await rw!.WriteLineAsync("LOGOUT", ct).ConfigureAwait(false);
                }

                bool accept;
                GameSummary? summary;
                try
                {
                    (summary, accept) = await ReceiveGameSummaryAsync(ct).ConfigureAwait(false);
                }
                catch (LogoutException)
                {
                    break;
                }

                // summary が null のときは、おかしな summary が送られてきたということなので、無視
                if (summary is null) continue;

                var player = accept
                    ? await playerFactory.AgreeWith(summary, ct).ConfigureAwait(false)
                    : null;

                if (player is null)
                {
                    await rw!.WriteLineAsync($"REJECT", ct).ConfigureAwait(false);
                }
                else
                {
                    await rw!.WriteLineAsync($"AGREE", ct).ConfigureAwait(false);
                }

                // Start or Reject が来るのを待つ
                if (!await ReceiveGameStartAsync(summary, ct).ConfigureAwait(false))
                {
                    playerFactory.Rejected(summary);
                    continue;
                }

                if (player is null) throw new CsaServerException("REJECT がサーバに無視されました;;");

                await new GameLoop(
                    rw!, summary, keepAliveInterval, player, options.SendPv)
                    .StartAsync(ct)
                    .ConfigureAwait(false);
            }
        }

        async Task LoginAsync(CancellationToken ct)
        {
            await rw!.WriteLineAsync($"LOGIN {options.UserName} {options.Password}", ct).ConfigureAwait(false);
            while (true)
            {
                var message = await rw.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == $"LOGIN:{options.UserName} OK")
                {
                    return;
                }
                else if (message == "LOGIN:incorrect")
                {
                    throw new LoginFailedException("ログインできませんでした。");
                }
            }
        }

        // 以下共通処理

        private protected async Task WaitingForMessageAsync(string expected, CancellationToken ct)
        {
            while (true)
            {
                var message = await rw!.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == expected) break;
            }
        }

        private protected async Task<bool> ReceiveGameStartAsync(GameSummary summary, CancellationToken ct)
        {
            while (true)
            {
                var message = await rw!.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == $"START:{summary.GameId}") return true;
                else if (message.StartsWith($"REJECT:{summary.GameId}")) return false;
            }
        }

        /// <summary>
        /// ストリームの読み込み・書き込み時に処理を挟むためのラッパー
        /// </summary>
        private protected class ReaderWriterWrapper : IDisposable
        {
            readonly CancellableReaderWriter rw;
            ILogger<CsaClient> logger;

            public ReaderWriterWrapper(CancellableReaderWriter rw, ILogger<CsaClient> logger)
            {
                this.rw = rw;
                this.logger = logger;
            }

            /// <summary>
            /// キャンセル以外の例外を CsaServerException で包み <br/>
            /// message が null の場合や LOGOUT の場合は例外を新たに投げる <br/>
            /// ついでにログに受け取った文字列を記録
            /// </summary>
            public async Task<string> ReadLineAsync(CancellationToken ct)
            {
                string? message;
                try
                {
                    message = await rw.ReadLineAsync(ct).ConfigureAwait(false);
                }
                // OperationCanceledException 以外は CsaServerException で包む
                catch (Exception e) when (e is not OperationCanceledException oe || oe.CancellationToken != ct)
                {
                    throw new CsaServerException("サーバからのメッセージ待機中に例外が発生しました。", e);
                }
                if (message is null) throw new CsaServerException("サーバとの接続が切れました。");
                logger.LogTrace($"  < {message}");
                if (message.StartsWith("LOGOUT")) throw new LogoutException("ログアウトしました。");
                return message;
            }

            /// <summary>
            /// キャンセル以外の書き込み時の例外を CsaServerException で包む <br/>
            /// また、書き込んだ文字列をログに記録
            /// </summary>
            public async Task WriteLineAsync(string message, CancellationToken ct)
            {
                try
                {
                    await rw.WriteLineLFAsync(message, ct).ConfigureAwait(false);
                }
                // OperationCanceledException 以外は CsaServerException で包む
                catch (Exception e) when (e is not OperationCanceledException oe || oe.CancellationToken != ct)
                {
                    throw new CsaServerException("サーバへのメッセージ送信中に例外が発生しました。", e);
                }
                logger.LogTrace($"> {message}");
            }

            public void Dispose()
            {
                rw.Dispose();
            }
        }

        private protected class GameLoop
        {
            ReaderWriterWrapper rw;
            GameSummary summary;
            IPlayer player;
            Position pos;
            RemainingTime remainingTime;
            TimeSpan keepAliveInterval;
            DateTime lastWrite = DateTime.Now;
            SemaphoreSlim writeSem = new(1, 1);
            bool sendPv;

            public GameLoop(
                ReaderWriterWrapper rw, GameSummary summary, TimeSpan keepAliveInterval, IPlayer player, bool sendPv)
            {
                this.rw = rw;
                this.summary = summary;
                this.player = player;
                this.pos = new Position(summary.StartPos);
                this.remainingTime = new RemainingTime(summary.TimeRule.TotalTime);
                this.keepAliveInterval = keepAliveInterval;
                this.sendPv = sendPv;

                foreach (var (move, time) in summary.Moves!)
                {
                    UpdateRemainingTime(pos.Player, remainingTime, time, summary);
                    pos.DoMove(move);
                }
            }

            public async Task StartAsync(CancellationToken ct)
            {
                var endState = EndGameState.None;
                var result = GameResult.Censored;
                var thinkTask = Task.CompletedTask;
                using var thinkCanceler = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Exception? canceledException = null;

                try
                {
                    player.GameStart();

                    if (pos.Player == summary.Color)
                    {
                        thinkTask = SendMoveAsync(thinkCanceler.Token);
                    }

                    var readlineTask = rw.ReadLineAsync(ct);

                    while (true)
                    {
                        var finished = await Task
                            .WhenAny(readlineTask, thinkTask, Task.Delay(keepAliveInterval))
                            .ConfigureAwait(false);

                        if (finished == readlineTask)
                        {
                            var message = await readlineTask.ConfigureAwait(false);

                            if (GameResultTable.ContainsKey(message))
                            {
                                result = GameResultTable[message];
                                break;
                            }
                            else if (message == "#CHUDAN")
                            {
                                endState = EndGameState.Chudan;
                                break;
                            }

                            readlineTask = rw.ReadLineAsync(ct);

                            // 指し手
                            if (message.StartsWith("+") || message.StartsWith("-"))
                            {
                                await thinkTask.ConfigureAwait(false);

                                var (move, time) = Core.Csa.ParseMoveWithTime(message, pos);
                                if (!pos.IsLegalMove(move))
                                {
                                    continue;
                                }
                                NewMove(move, time);
                                if (pos.Player == summary.Color)
                                {
                                    thinkTask = SendMoveAsync(thinkCanceler.Token);
                                }
                            }
                            // 投了、入玉宣言
                            else if (message.StartsWith("%"))
                            {
                                // throw new NotImplementedException();
                            }
                            else if (EndGameStateTable.ContainsKey(message))
                            {
                                endState = EndGameStateTable[message];
                            }
                            // これ以外は無視
                        }
                        // finished == thinkTask
                        else if (finished == thinkTask)
                        {
                            // 例外をスロー
                            if (!thinkTask.IsCompletedSuccessfully) await thinkTask;
                        }
                        // KeepAliveInterval 経っても何も起きていないとき
                        else
                        {
                            await writeSem.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                //　ちゃんと 30 秒以上経っているか確認
                                if ((DateTime.Now - lastWrite) >= keepAliveInterval)
                                {
                                    await rw.WriteLineAsync("", ct).ConfigureAwait(false);
                                    lastWrite = DateTime.Now;
                                }
                            }
                            finally
                            {
                                writeSem.Release();
                            }
                        }
                    }
                }
                catch (Exception e) when (e is CsaServerException
                    || (e is OperationCanceledException ex && ex.CancellationToken == ct))
                {
                    canceledException = e;
                }

                if (!thinkTask.IsCompleted && !ct.IsCancellationRequested)
                    thinkCanceler.Cancel();
                try
                {
                    await thinkTask.ConfigureAwait(false);
                }
                // キャンセル例外は無視
                catch (Exception e) when (e is CsaServerException
                    || (e is OperationCanceledException ex
                        && (ex.CancellationToken == ct || ex.CancellationToken == thinkCanceler.Token)))
                {
                }
                player.GameEnd(endState, result);

                if (canceledException is { } exception) throw exception;
            }

            async Task SendMoveAsync(CancellationToken ct)
            {
                var clonedPos = pos.Clone();
                var (bestmove, eval, pv)
                    = await player.ThinkAsync(pos.Clone(), remainingTime.Clone(), ct).ConfigureAwait(false);

                if (!(clonedPos.IsLegalMove(bestmove) || bestmove == Move.Resign || bestmove == Move.Win))
                {
                    throw new PlayerException($"合法手ではありません。指し手={bestmove.Usi()}, 局面={clonedPos.Sfen()}");
                }

                var message = new StringBuilder(bestmove == Move.Resign ? "%TORYO"
                    : bestmove == Move.Win ? "%KACHI"
                    : bestmove.Csa(clonedPos));

                if (sendPv
                    && bestmove != Move.Resign
                    && bestmove != Move.Win
                    && (eval is not null || pv is not null))
                {
                    message.Append(",'*");
                    if (eval is not null)
                    {
                        message.Append(' ');
                        message.Append(eval);
                    }
                    if (pv is not null)
                    {
                        foreach (var m in pv)
                        {
                            if (!clonedPos.IsLegalMove(m)) break;
                            message.Append(' ');
                            message.Append(m.Csa(clonedPos));
                            clonedPos.DoMove(m);
                        }
                    }
                }

                await writeSem.WaitAsync().ConfigureAwait(false);
                try
                {
                    await rw.WriteLineAsync(message.ToString(), ct).ConfigureAwait(false);
                    lastWrite = DateTime.Now;
                }
                finally
                {
                    writeSem.Release();
                }
            }

            void NewMove(Move move, TimeSpan time)
            {
                UpdateRemainingTime(pos.Player, remainingTime, time, summary);
                pos.DoMove(move);
                player.NewMove(move, time);
            }

            static void UpdateRemainingTime(Color c, RemainingTime rem, TimeSpan elapsed, GameSummary summary)
            {
                rem[c] += summary.TimeRule.Increment - elapsed;
                if (rem[c] < TimeSpan.Zero)
                {
                    rem[c] += summary.TimeRule.Byoyomi;
                    if (rem[c] > TimeSpan.Zero) rem[c] = TimeSpan.Zero;
                }
            }

            static readonly Dictionary<string, GameResult> GameResultTable = new Dictionary<string, GameResult>
            {
                { "#WIN", GameResult.Win },
                { "#LOSE", GameResult.Lose },
                { "#DRAW", GameResult.Draw },
                { "#CENSORED", GameResult.Censored },
            };

            static readonly Dictionary<string, EndGameState> EndGameStateTable = new Dictionary<string, EndGameState>
            {
                { "#SENNICHITE", EndGameState.Sennichite },
                { "#OUTE_SENNICHITE", EndGameState.OuteSennichite },
                { "#ILLEGAL_MOVE", EndGameState.IllegalMove },
                { "#TIME_UP", EndGameState.TimeUp },
                { "#RESIGN", EndGameState.Resign },
                { "#JISHOGI", EndGameState.Jishogi },
                { "#MAX_MOVES", EndGameState.MaxMoves },
                { "#ILLEGAL_ACTION", EndGameState.IllegalAction },
            };
        }

        async Task SetIsWaitingForNextGameAsync(bool v, CancellationToken ct)
        {
            await stateSem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                isWaitingForNextGame = v;
            }
            finally { stateSem.Release(); }
        }

        // 一旦全部 StringBuilder とかに突っ込んであとからパースする方が良かったかも...
        private protected async Task<(GameSummary? summary, bool accept)> ReceiveGameSummaryAsync(CancellationToken ct)
        {
            await SetIsWaitingForNextGameAsync(true, ct).ConfigureAwait(false);
            await WaitingForMessageAsync("BEGIN Game_Summary", ct).ConfigureAwait(false);
            await SetIsWaitingForNextGameAsync(false, ct).ConfigureAwait(false);

            string? protocolVersion = null;
            string protocolMode = "Server";
            string? format = null;
            string? declaration = null;
            string gameId = "";
            string? blackName = null;
            string? whiteName = null;
            Color? color = null;
            bool rematchOnDraw = false;
            Color? startColor = null;
            int? maxMoves = null;

            while (true)
            {
                var message = await rw!.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == "BEGIN Time") break;

                var sp = message.Split(':');

                if (sp.Length < 2) continue;

                switch (sp[0])
                {
                    case "Protocol_Version":
                        protocolVersion = sp[1];
                        break;
                    case "Protocol_Mode":
                        protocolMode = sp[1];
                        break;
                    case "Format":
                        format = sp[1];
                        break;
                    case "Declaration":
                        declaration = sp[1];
                        break;
                    case "Game_ID":
                        gameId = sp[1];
                        break;
                    case "Name+":
                        blackName = sp[1];
                        break;
                    case "Name-":
                        whiteName = sp[1];
                        break;
                    case "Your_Turn":
                        {
                            if (Core.Csa.TryParseColor(sp[1], out var c)) color = c;
                            break;
                        }
                    case "Rematch_On_Draw":
                        rematchOnDraw = sp[1] == "YES";
                        break;
                    case "To_Move":
                        {
                            if (Core.Csa.TryParseColor(sp[1], out var c)) startColor = c;
                            break;
                        }
                    case "Max_Moves":
                        {
                            if (int.TryParse(sp[1], out var v)) maxMoves = v;
                            break;
                        }
                }
            }

            TimeSpan timeUnit = TimeSpan.FromSeconds(1.0);
            int leastTime = 0;
            bool isRoundUp = false;
            int? totalTime = null;
            int byoyomi = 0;
            int delay = 0;
            int increment = 0;

            while (true)
            {
                var message = await rw.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == "END Time") break;

                var sp = message.Split(':');

                if (sp.Length < 2) continue;

                if (sp[0] == "Time_Unit")
                {
                    // 単位（ミリ秒）
                    var d = sp[1].TakeWhile(c => '0' <= c && c <= '9').Count();
                    var unit = sp[1].EndsWith("min") ? 1000 * 60
                        : sp[1].EndsWith("msec") ? 1
                        : 1000;
                    if (int.TryParse(sp[1][0..d], out var num))
                    {
                        timeUnit = TimeSpan.FromMilliseconds(num * unit);
                    }
                }
                else if (sp[0] == "Time_Roundup")
                {
                    isRoundUp = sp[1] == "YES";
                }
                else if (int.TryParse(sp[1], out var v))
                {
                    switch (sp[0])
                    {
                        case "Least_Time_Per_Move":
                            leastTime = v;
                            break;
                        case "Total_Time":
                            totalTime = v;
                            break;
                        case "Byoyomi":
                            byoyomi = v;
                            break;
                        case "Delay":
                            delay = v;
                            break;
                        case "Increment":
                            increment = v;
                            break;
                    }
                }
            }

            await WaitingForMessageAsync("BEGIN Position", ct).ConfigureAwait(false);

            var lines = new Queue<string>();
            while (true)
            {
                var message = await rw.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == "END Position") break;
                lines.Enqueue(message);
            }

            var startpos = Core.Csa.ParseBoard(lines);
            var movesWithTime = ParseMovesWithTime(lines, startpos);

            await WaitingForMessageAsync("END Game_Summary", ct).ConfigureAwait(false);

            if (protocolVersion is null
                || format is null
                || declaration is null
                || blackName is null
                || whiteName is null
                || color is null
                || startColor is null
                || totalTime is null)  // 時間制限無しはとりあえず未対応
                return (null, false);

            var timeRule = new TimeRule
            {
                TimeUnit = timeUnit,
                LeastTimePerMove = leastTime * timeUnit,
                TotalTime = (int)totalTime * timeUnit,
                Byoyomi = byoyomi * timeUnit,
                Delay = delay * timeUnit,
                Increment = increment * timeUnit,
                IsRoundUp = isRoundUp,
            };
            var summary = new GameSummary
            (
                GameId: gameId,
                BlackName: blackName,
                WhiteName: whiteName,
                Color: (Color)color,
                StartColor: (Color)startColor,
                MaxMoves: maxMoves,
                TimeRule: timeRule,
                StartPos: startpos,
                Moves: movesWithTime
            );

            if (!(protocolVersion == "1.1" || protocolVersion == "1.2")
                || format != "Shogi 1.0"
                || declaration != "Jishogi 1.1"
                || rematchOnDraw)      // false のみ受け付ける(trueの場合の動作がプロトコルに明記されていないため)
                return (summary, false);

            return (summary, true);
        }

        static List<(Move, TimeSpan)> ParseMovesWithTime(Queue<string> lines, Board startpos)
        {
            var pos = new Position(startpos);
            var moves = new List<(Move, TimeSpan)>();

            while (true)
            {
                if (!lines.TryDequeue(out var moveStr)
                    || !(moveStr.StartsWith("+") || moveStr.StartsWith("-"))) break;

                var (move, time) = Core.Csa.ParseMoveWithTime(moveStr, pos);
                moves.Add((move, time));
                pos.DoMove(move);
            }

            return moves;
        }
    }
}