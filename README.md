# ShogiLibSharp
[![build and test](https://github.com/tomori-k/ShogiLibSharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/tomori-k/ShogiLibSharp/actions/workflows/build-and-test.yml)

C# 用の将棋ライブラリです。

## 機能
### ShogiLibSharp.Csa

CSA サーバプロトコルでサーバ対局を行うクライアントを簡単に実装できるライブラリです。

``` cs
// floodgate で N 回対局してログアウトするプログラムの例
// 接続切れなどの例外処理を行っていないので注意

using ShogiLibSharp.Core;
using ShogiLibSharp.Engine;
using ShogiLibSharp.Csa;

// 設定項目
const int N = 4;
const string engineFilePath = @"[エンジンのファイルパス]";
const string username = "[ログイン名]";
const string password = "[パスワード]";

// 接続設定
var connectOptions = new ShogiServerOptions
(
   HostName: "wdoor.c.u-tokyo.ac.jp",
   UserName: username,
   Password: password,
   GameName: "floodgate-300-10F"
);

using var engine = new UsiEngine(engineFilePath);

// エンジン起動
await engine.BeginAsync();

// 接続開始
var client = new ShogiServerClient(new PlayerFactory(N, engine), connectOptions);

// 切断まで待つ
await client.ConnectionTask;

// エンジン終了
await engine.QuitAsync();

// 対局条件への合意やログイン続行の条件などを記述するクラス
class PlayerFactory : IPlayerFactory
{
    readonly int maxGameCount;
    readonly UsiEngine engine;

    int gameCount = 0;

    public PlayerFactory(int maxGameCount, UsiEngine engine)
    {
        this.maxGameCount = maxGameCount;
        this.engine = engine;
    }

    public async Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct)
    {
        await engine.IsReadyAsync(ct);
        return new Player(summary, engine);
    }

    public bool ContinueLogin()
    {
        return gameCount++ < maxGameCount;
    }

    public void Rejected(GameSummary summary)
    {
        gameCount--;
    }
}

// 思考部分の処理を実装するクラス
class Player : IPlayer
{
    readonly TimeRule timeRule;
    readonly UsiEngine engine;

    public Player(GameSummary summary, UsiEngine engine)
    {
        this.timeRule = summary.TimeRule;
        this.engine = engine;
    }

    // 対局終了時の処理を記述
    public void GameEnd(EndGameState endState, GameResult result)
    {
        var message = result == GameResult.Win ? "win"
            : result == GameResult.Lose ? "lose"
            : "draw";
        engine.Gameover(message);
    }

    // 対局開始時の処理を記述
    public void GameStart()
    {
        engine.StartNewGame();
    }

    // サーバから指し手が送信されてきたときの処理を記述
    public void NewMove(Move move, TimeSpan elapsed)
    {
    }

    // 手番が回ってきたときの処理を記述
    public async Task<(Move Bestmove, long? Eval, List<Move>? Pv)>
        ThinkAsync(Position pos, RemainingTime time, CancellationToken ct)
    {
        var limits = SearchLimit.Create(
            time[Color.Black], time[Color.White], timeRule.Increment, timeRule.Increment);
        var (bestmove, ponder) = await engine.GoAsync(pos, limits, ct);
        return (bestmove, null, null);
    }
}
```