using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            moves.RemoveAll(m => m.IsSuicideMove(pos) || m.IsUchifuzume(pos));

            return moves;
        }

        private static bool IsSuicideMove(this Move m, Position pos)
        {
            if (pos.InCheck())
            {
                return IsSuicideMoveInCheck(m, pos);
            }

            if (m.IsDrop())
            {
                return false;
            }

            if (pos.PieceAt(m.From()).Colorless() == Piece.King)
            {
                return pos
                    .EnumerateAttackers(pos.Player.Opponent(), m.To())
                    .Any();
            }
            else
            {
                var pinned = pos.PinnedBy(pos.Player);
                if (!pinned.Test(m.From()))
                {
                    return false;
                }
                var ksq = pos.King(pos.Player);
                var movable = Bitboard.Line(ksq, m.From());
                return !movable.Test(m.To());
            }
        }

        private static bool IsSuicideMoveInCheck(this Move m, Position pos)
        {
            var checkers = pos.Checkers();
            var checkerCount = checkers.Popcount();
            var ksq = pos.King(pos.Player);

            Debug.Assert(checkerCount > 0);

            if (checkerCount == 1)
            {
                if (m.IsDrop())
                {
                    var csq = checkers.LsbSquare();
                    return !Bitboard.Between(ksq, csq).Test(m.To());
                }
                else if (pos.PieceAt(m.From()) == Piece.King)
                {
                    Debug.Assert(ksq == m.From());
                    return IsSuicideKingMove(ksq, m.To(), pos);
                }
                else
                {
                    var csq = checkers.LsbSquare();
                    var to = Bitboard.Between(ksq, csq) | checkers;
                    if (!to.Test(m.To()))
                    {
                        return true;
                    }
                    return pos
                        .PinnedBy(pos.Player.Opponent())
                        .Test(m.From());
                }
            }
            else
            {
                if (m.IsDrop() || pos.PieceAt(m.From()).Colorless() != Piece.King)
                {
                    return true;
                }
                Debug.Assert(ksq == m.From());
                return IsSuicideKingMove(ksq, m.To(), pos);
            }
        }

        private static bool IsSuicideKingMove(int from, int to, Position pos)
        {
            var attackers = pos
                .EnumerateAttackers(pos.Player.Opponent(), to, pos.GetOccupancy() ^ from);
            return attackers.Any();
        }

        private static bool IsUchifuzume(this Move m, Position pos)
        {
            if (!(m.IsDrop() && m.Dropped() == Piece.Pawn))
            {
                return false;
            }

            var to = m.To();
            var theirKsq = pos.King(pos.Player.Opponent());

            if (!Bitboard.PawnAttacks(pos.Player, to)
                .Test(theirKsq))
            {
                return false;
            }

            var defenders = pos.EnumerateAttackers(
                pos.Player.Opponent(), to) ^ theirKsq;

            if (defenders.Any())
            {
                var pinned = pos.PinnedBy(pos.Player);
                if (defenders.AndNot(pinned).Any())
                {
                    return false;
                }
                // 現在ピンされていても、歩を打つことで
                // ピンが解除される位置なら防御可能
                defenders &= Bitboard.Line(theirKsq, to);
                if (defenders.Any())
                {
                    return false;
                }
            }

            var occ = pos.GetOccupancy() ^ to;
            var evasionTo = Bitboard.KingAttacks(theirKsq)
                .AndNot(pos.ColorBB(pos.Player.Opponent()));
            
            foreach (var kTo in evasionTo)
            {
                var attackers = pos
                    .EnumerateAttackers(pos.Player, kTo, occ);
                if (attackers.None())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
