using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Csa
{
    // todo: 名前再考
    public interface IPlayerFactory
    {
        bool ContinueLogin();
        Task<IPlayer?> AgreeWith(GameSummary summary, CancellationToken ct); // 対局するならIPlayer を返し、そうでないなら null を返す
    }

    public interface IPlayer
    {
        void GameStart();
        void NewMove(Move move, TimeSpan elapsed);
        void GameEnd(EndGameState endState, GameResult result);
        Task<Move> ThinkAsync(Position pos, RemainingTime time, CancellationToken ct); // 思考可能時間: time[us] + summary.Increment + summary.Byoyomi
    }
}
