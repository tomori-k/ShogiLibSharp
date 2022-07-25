using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    internal static class UsiCommand
    {
        public static (Move, Move) ParseBestmove(string command)
        {
            var sp = command.Split();
            if (sp.Length < 2)
            {
                throw new FormatException($"USI 形式の bestmove コマンドではありません。：{command}");
            }
            return sp.Length < 4
                ? (Usi.ParseMove(sp[1]), Move.None)
                : (Usi.ParseMove(sp[1]), Usi.ParseMove(sp[3]));
        }

        public static void NotifyBestmoveReceived(TaskCompletionSource<(Move, Move)> tcs, string command)
        {
            try
            {
                var (move, ponder) = ParseBestmove(command);
                tcs.SetResult((move, ponder));
            }
            catch (FormatException e)
            {
                tcs.SetException(e);
            }
        }
    }
}
