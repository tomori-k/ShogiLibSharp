using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    internal static class Misc
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

        public static void SendGo(this StreamWriter sw, string sfenWithMoves, SearchLimit limits, bool ponder = false)
        {
            var ponderEnabled = ponder ? " ponder" : "";
            sw.WriteLine($"position {sfenWithMoves}");
            if (limits.Binc == 0 && limits.Winc == 0)
            {
                sw.WriteLine($"go{ponderEnabled} btime {limits.Btime} wtime {limits.Wtime} byoyomi {limits.Byoyomi}");
            }
            else
            {
                sw.WriteLine($"go{ponderEnabled} btime {limits.Btime} wtime {limits.Wtime} binc {limits.Binc} winc {limits.Winc}");
            }
        }
    }
}
