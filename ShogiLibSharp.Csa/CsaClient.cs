using ShogiLibSharp.Core;
using ShogiLibSharp.Csa.Exceptions;
using System.Diagnostics;
using System.Net.Sockets;

namespace ShogiLibSharp.Csa
{
    public class CsaClient : IDisposable
    {
        TcpClient tcp = new();
        WrapperStream? stream = null;
        ConnectOptions Options;
        bool disposed = false;
        CancellationTokenSource lifetime = new();

        public CsaClient(ConnectOptions options) { Options = options; }

        public void Dispose()
        {
            if (disposed) return;
            lifetime.Cancel();
            lifetime.Dispose();
            stream!?.Dispose();
            tcp.Dispose();
            disposed = true;
        }

        public async Task ConnectAsync(IPlayerFactory playerFactory, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, ct);
            try
            {
                await ConnectAsyncImpl(playerFactory, cts.Token);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                if (lifetime.IsCancellationRequested)
                {
                    throw new OperationCanceledException("CsaClient が Dispose() されました。", e, lifetime.Token);
                }
                else
                {
                    throw new OperationCanceledException(e.Message, e, ct);
                }
            }
        }

        async Task ConnectAsyncImpl(IPlayerFactory playerFactory, CancellationToken ct)
        {
            await tcp.ConnectAsync(Options.HostName, Options.Port, ct).ConfigureAwait(false);

            stream = new WrapperStream(tcp.GetStream());

            await LoginAsync(ct).ConfigureAwait(false);

            while (true)
            {
                GameSummary? summary;
                //try
                //{
                    summary = await ReceiveGameSummaryAsync(ct).ConfigureAwait(false);
                    if (summary is null) continue;
                //}
                //catch (OperationCanceledException)
                //{
                //    // Dispose() されたのでなければ、ログアウト処理
                //    if (lifetime.IsCancellationRequested) throw;
                //    await LogoutAsync();
                //    break;
                //}
            
                // Agree or Reject
                // AgreeWith の結果が null なら Reject
                if (await playerFactory.AgreeWith(summary, ct).ConfigureAwait(false) is not { } player)
                {
                    await WriteLineAsync(stream, $"REJECT", ct).ConfigureAwait(false);
                    continue;
                }
                await WriteLineAsync(stream, $"AGREE", ct).ConfigureAwait(false);

                // 相手側の Reject
                if (!await ReceiveGameStartAsync(summary, ct).ConfigureAwait(false))
                {
                    continue;
                }

                await new GameLoop(stream, summary, player).StartAsync(ct).ConfigureAwait(false);
            }
        }

        async Task LoginAsync(CancellationToken ct)
        {
            await WriteLineAsync(stream!, $"LOGIN {Options.UserName} {Options.Password}", ct).ConfigureAwait(false);
            while (true)
            {
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == $"LOGIN:{Options.UserName} OK")
                {
                    return;
                }
                else if (message == "LOGIN:incorrect")
                {
                    throw new LoginFailedException("ログインできませんでした。");
                }
            }
        }

        //async Task LogoutAsync()
        //{
        //    using var logoutCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        //    logoutCts.CancelAfter(TimeSpan.FromSeconds(10.0)); // 10 秒待ってログアウト出来なかったら強制的に切る

        //    try
        //    {
        //        await stream!.WriteLineLFAsync("LOGOUT", logoutCts.Token).ConfigureAwait(false);
        //        await WaitingForMessageAsync("LOGOUT:completed", logoutCts.Token).ConfigureAwait(false);
        //    }
        //    catch (OperationCanceledException e) when (e.CancellationToken == logoutCts.Token)
        //    {
        //        if (lifetime.IsCancellationRequested)
        //        {
        //            throw new OperationCanceledException("CsaClient が Dispose() されました。", e, lifetime.Token);
        //        }
        //        else
        //        {
        //            throw new OperationCanceledException("LOGOUT 処理が一定時間内に終了しなかったため、強制的に切断しました。");
        //        }
        //    }
        //}

        async Task WaitingForMessageAsync(string expected, CancellationToken ct)
        {
            while (true)
            {
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == expected) break;
            }
        }

        async Task<bool> ReceiveGameStartAsync(GameSummary summary, CancellationToken ct)
        {
            while (true)
            {
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == $"START:{summary.GameId}") return true;
                else if (message == $"REJECT:{summary.GameId}") return false;
            }
        }

        static async Task WriteLineAsync(WrapperStream stream, string message, CancellationToken ct)
        {
            try
            {
                await stream.WriteLineLFAsync(message, ct).ConfigureAwait(false);
            }
            // OperationCanceledException 以外は CsaServerException で包む
            catch (Exception e) when (e is not OperationCanceledException oe || oe.CancellationToken != ct)
            {
                throw new CsaServerException("サーバからのメッセージ待機中に例外が発生しました。", e);
            }
        }

        /// <summary>
        /// キャンセル例外はそのまま、それ以外の例外を CsaServerException で包む
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="CsaServerException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        static async Task<string> ReadLineAsync(WrapperStream stream, CancellationToken ct)
        {
            string? message;
            try
            {
                message = await stream.ReadLineAsync(ct).ConfigureAwait(false);
            }
            // OperationCanceledException 以外は CsaServerException で包む
            catch (Exception e) when (e is not OperationCanceledException oe || oe.CancellationToken != ct)
            {
                throw new CsaServerException("サーバからのメッセージ待機中に例外が発生しました。", e);
            }
            if (message is null) throw new CsaServerException("サーバとの接続が切れました。");
            return message;
        }

        class GameLoop
        {
            WrapperStream stream;
            GameSummary summary;
            IPlayer player;
            Position pos;
            RemainingTime remainingTime;

            public GameLoop(
                WrapperStream stream, GameSummary summary, IPlayer player)
            {
                this.stream = stream;
                this.summary = summary;
                this.player = player;
                this.pos = summary.StartPos!.Clone();
                this.remainingTime = new RemainingTime(summary.TimeRule!.TotalTime);

                foreach (var (move, time) in summary.Moves!)
                {
                    NewMove(move, time);
                }
            }

            public async Task StartAsync(CancellationToken ct)
            {
                var endState = EndGameState.None;
                var result = GameResult.Censored;
                var thinkTask = Task.CompletedTask;
                using var thinkCanceler = CancellationTokenSource.CreateLinkedTokenSource(ct);

                try
                {
                    player.GameStart();

                    if (pos.Player == summary.Color)
                    {
                        thinkTask = SendMoveAsync(thinkCanceler.Token);
                    }

                    var readlineTask = ReadLineAsync(stream, ct);

                    while (true)
                    {
                        var finished = await Task
                            .WhenAny(readlineTask, thinkTask)
                            .ConfigureAwait(false);

                        if (finished == readlineTask)
                        {
                            var message = await readlineTask;

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

                            readlineTask = ReadLineAsync(stream, ct);

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
                        else
                        {
                            // 例外をスロー
                            if (!thinkTask.IsCompletedSuccessfully) await thinkTask;
                        }
                    }
                    player.GameEnd(endState, result);
                }
                catch (Exception e) when (e is CsaServerException
                    || (e is OperationCanceledException ex && ex.CancellationToken == ct))
                {
                    if (!thinkTask.IsCompleted && !ct.IsCancellationRequested)
                    {
                        thinkCanceler.Cancel();
                    }
                    try
                    {
                        await thinkTask.ConfigureAwait(false);
                    }
                    // キャンセル例外は無視
                    catch (Exception e1) when (e1 is CsaServerException
                        || (e1 is OperationCanceledException oe1
                            && (oe1.CancellationToken == ct || oe1.CancellationToken == thinkCanceler.Token)))
                    {
                    }
                    player.GameEnd(endState, result);
                    throw;
                }
            }

            async Task SendMoveAsync(CancellationToken ct)
            {
                var bestmove = await player.ThinkAsync(pos.Clone(), remainingTime.Clone(), ct).ConfigureAwait(false);
                var csa = bestmove == Move.Resign ? "%TORYO"
                    : bestmove == Move.Win ? "%KACHI"
                    : bestmove.Csa(pos);
                await WriteLineAsync(stream, csa, ct).ConfigureAwait(false);
            }

            void NewMove(Move move, TimeSpan time)
            {
                // 思考可能時間=持ち時間+秒読み+inc としておき、手番交代のときに増加分を足すことにする
                remainingTime[pos.Player] += summary.TimeRule!.Increment;
                remainingTime[pos.Player] -= time;
                pos.DoMove(move);
                player.NewMove(move, time);
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

        async Task<GameSummary?> ReceiveGameSummaryAsync(CancellationToken ct)
        {
            await WaitingForMessageAsync("BEGIN Game_Summary", ct).ConfigureAwait(false);

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
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == "BEGIN Time") break;

                var sp = message!.Split(':');

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
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == "END Time") break;

                var sp = message!.Split(':');

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
                var message = await ReadLineAsync(stream!, ct).ConfigureAwait(false);
                if (message == "END Position") break;
                lines.Enqueue(message!);
            }

            var startpos = Core.Csa.ParseStartPosition(lines);
            var movesWithTime = Core.Csa.ParseMovesWithTime(lines, startpos);

            await WaitingForMessageAsync("END Game_Summary", ct).ConfigureAwait(false);

            if (!(protocolVersion == "1.1" || protocolVersion == "1.2")
                || format != "Shogi 1.0"
                || declaration != "Jishogi 1.1"
                || blackName is null
                || whiteName is null
                || color is null
                || rematchOnDraw       // false のみ対応
                || startColor is null
                || totalTime is null)  // 時間制限無しはとりあえず未対応
                return null;

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
            return new GameSummary
            {
                GameId = gameId,
                BlackName = blackName,
                WhiteName = whiteName,
                Color = (Color)color,
                StartColor = (Color)startColor,
                MaxMoves = maxMoves,
                TimeRule = timeRule,
                StartPos = startpos,
                Moves = movesWithTime,
            };
        }
    }
}