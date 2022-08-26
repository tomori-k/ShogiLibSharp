# ShogiLibSharp

[![build and test](https://github.com/tomori-k/ShogiLibSharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/tomori-k/ShogiLibSharp/actions/workflows/build-and-test.yml)

C# の将棋ライブラリです。

## 機能

### ShogiLibSharp.Core

指し手生成など、基本的な機能を提供するライブラリです。

``` cs
using ShogiLibSharp.Core;

var pos = new Position("lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1"); // 平手

Console.WriteLine(pos.Sfen()); // lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1"

pos.DoMove(Usi.ParseMove("2g2f"));

Console.WriteLine(pos.Player == Color.White); // true

pos.UndoMove();

Console.WriteLine(pos.InCheck()); // false
Console.WriteLine(pos.IsMated()); // false
Console.WriteLine(pos.IsLegalMove(Usi.ParseMove("7g7f"))); // true
Console.WriteLine(pos.CheckRepetition() == Repetition.None); // true
Console.WriteLine(pos.CanDeclareWin()); // false

foreach (var m in Movegen.GenerateMoves(pos))
{
    Console.WriteLine(m.Usi());
}

```

### ShogiLibSharp.Engine

USI プロトコルに対応したエンジンを簡単に扱えるライブラリです。

``` cs
using ShogiLibSharp.Core;
using ShogiLibSharp.Engine;

const string engineFilePath = @"[エンジンのファイルパス]";
using var engine = new UsiEngine(engineFilePath);

await engine.BeginAsync();
await engine.IsReadyAsync();

engine.StartNewGame();

var pos = new Position(Position.Hirate);
var limits = SearchLimit.Create(TimeSpan.FromSeconds(10.0));

var result = await engine.GoAsync(pos, limits);

Console.WriteLine(result.Bestmove);
Console.WriteLine(result.Ponder);
var info = result.InfoList.LastOrDefault();

engine.Gameover("win");

await engine.QuitAsync();

```

### ShogiLibSharp.Kifu

CSA や KIF などの形式の棋譜ファイルのパーサを提供するライブラリです。

``` cs
using ShogiLibSharp.Core;
using ShogiLibSharp.Kifu;

var kifu1 = Csa.Parse("[CSA 棋譜ファイルパス]", Encoding.UTF8);
var kifu2 = Kif.Parse("[KIF 棋譜ファイルパス]", Encoding.UTF8);

Console.WriteLine(kifu1.GameInfo.Names[0]);
Console.WriteLine(new Position(kifu1.StartPos).Sfen());
for (var mi in kifu1.MoveLists[0])
{
    Console.WriteLine(mi.Move.Usi());
    Console.WriteLine(mi.Elapsed);
    Console.WriteLine(mi.Comment);
}
```

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
        var result = await engine.GoAsync(pos, limits, ct);
        return (result.Bestmove, null, null);
    }
}
```

## ライセンス

* MIT License
