using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    /// <summary>
    /// ログイン状態を管理する型を表すインターフェース
    /// </summary>
    public interface IPlayerFactory
    {
        bool ContinueLogin();
        void Rejected(GameSummary summary);
        Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct); // 対局するなら IPlayer を返し、そうでないなら null を返す
    }

    /// <summary>
    /// 対局中の処理を行う型を表すインターフェース
    /// </summary>
    public interface IPlayer
    {
        void GameStart();
        void NewMove(Move move, TimeSpan elapsed);
        void GameEnd(EndGameState endState, GameResult result);
        Task<(Move Bestmove, long? Eval, List<Move>? Pv)>
            ThinkAsync(Position pos, RemainingTime time, CancellationToken ct); // 思考可能時間: time[us] + summary.Increment + summary.Byoyomi
    }
}
