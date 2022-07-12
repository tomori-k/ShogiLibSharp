using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiLibSharp
{
    public static class Movegen
    {
        private static void AddMovesToList(Piece p, int from, int to, List<Move> moves)
        {
            var c = p.Color();
            p = p.Colorless();

            if ((Square.RankOf(c, to) <= 1 && p == Piece.Knight)
                || (Square.RankOf(c, to) == 0 && (p == Piece.Pawn || p == Piece.Lance)))
            {
                moves.Add(Move.MakeMove(from, to, true));
            }
            else
            {
                moves.Add(Move.MakeMove(from, to, false));

                if (Square.CanPromote(c, from, to)
                    && !(p.IsPromoted() || p == Piece.Gold || p == Piece.King))
                    moves.Add(Move.MakeMove(from, to, true));
            }
        }

        /// <summary>
        /// pos における合法手を生成
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static List<Move> GenerateMoves(Position pos)
        {
            var moves = new List<Move>();
            var occupancy = pos.GetOccupancy();
            var us = pos.ColorBB(pos.Player);

            // 駒移動
            foreach (var from in us)
            {
                Piece p = pos.PieceAt(from);
                Bitboard to_bb = Bitboard.Attacks(p, from, occupancy) & ~us;
                foreach (int to in to_bb)
                {
                    AddMovesToList(p, from, to, moves);
                }
            }

            // 駒打ち
            foreach (Piece p in PieceExtensions.PawnToRook)
            {
                var captures = pos.CaptureListOf(pos.Player);
                if (captures.Count(p) > 0)
                {
                    var to_bb = ~occupancy & Bitboard.ReachableMask(pos.Player, p);
                    if (p == Piece.Pawn)
                    {
                        var pawns = pos.PieceBB(Piece.Pawn.Colored(pos.Player));
                        to_bb &= Bitboard.PawnDropMask(pawns);
                    }
                    foreach (int to in to_bb)
                    {
                        moves.Add(Move.MakeDrop(p, to));
                    }
                }
            }

            // 非合法手（自殺手、打ち歩詰め）を省く
            moves.RemoveAll(m => pos.IsSuicideOrUchifuzume(m));

            return moves;
        }
    }
}
