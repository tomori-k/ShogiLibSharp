using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ShogiLibSharp
{
    public static class Perft
    {
        public static (ulong, TimeSpan) Go(int depth, string sfen)
        {
            var pos = new Position(sfen);
            var sw = new Stopwatch();

            sw.Start();
            var nodes = PerftImpl(pos, depth);
            sw.Stop();

            return (nodes, sw.Elapsed);
        }

        public static ulong PerftImpl(Position pos, int depth)
        {
            var moves = Movegen.GenerateMoves(pos);

            if (depth == 1)
                return (ulong)moves.Count;

            ulong count = 0;

            foreach(Move m in moves)
            {
                pos.DoMove_PseudoLegal(m);
                count += PerftImpl(pos, depth - 1);
                pos.UndoMove();
            }

            return count;
        }
    }
}