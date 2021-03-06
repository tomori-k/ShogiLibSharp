using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ShogiLibSharp.MovegenGenerator;

namespace ShogiLibSharp.Core
{
    public static partial class Movegen
    {
        /// <summary>
        /// pos における合法手を生成
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static partial List<Move> GenerateMoves(Position pos);

        /*
         * 指し手生成を高速化するため、source generator によって
         * 元になるコード（Implで終わるメソッド）から実際の
         * 生成メソッドを自動生成する。
         * 
         * [InlineBitboardEnumerator]:
         * この属性を付けたメソッド内のBitboard に対する foreach を すべて
         * while で置き換えた別メソッド（Bitboard.GetEnumerator() を使わない）
         * を生成する。
         * 対象となるメソッドの名前は Impl で終わる必要があり、生成される
         * メソッドの名前はその Impl を取り除いたものとなる。
         * 
         * [MakePublic]:
         * [InlineBitboardEnumerator] によって foreach を置き換えるとき、
         * そのメソッドが private なら public 変更する。
         * 
         * 注意：
         * 属性名に、"Attribute" の suffix をつけたり
         * 名前空間をつけたり（MovegenGenerator.MakePublic など）
         * すると、ジェネレータが上手く動かないので注意する（手抜き）
         */

        [InlineBitboardEnumerator, MakePublic]
        private static List<Move> GenerateMovesImpl(Position pos)
        {
            if (pos.InCheck()) return GenerateEvasionMoves(pos);

            var moves = new List<Move> {  };
            var occupancy = pos.GetOccupancy();
            var us = pos.ColorBB(pos.Player);
            var pinned = pos.PinnedBy(pos.Player.Opponent()) & us;

            // 歩
            {
                var fromBB = pos.PieceBB(pos.Player, Piece.Pawn).AndNot(pinned);
                var toBB = (pos.Player == Color.Black
                    ? fromBB >> 1 : fromBB << 1)
                    .AndNot(us);
                var delta = pos.Player == Color.Black ? 1 : -1;
                foreach (var to in toBB)
                {
                    var from = to + delta;
                    var rank = Square.RankOf(pos.Player, to);
                    if (rank == 0)
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to, true));
                    }
                    else if (rank <= 2)
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to, false));
                        moves.Add(MoveExtensions.MakeMove(from, to, true));
                    }
                    else
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to, false));
                    }
                }
            }

            // 香
            {
                var fromBB = pos.PieceBB(pos.Player, Piece.Lance).AndNot(pinned);
                foreach (var from in fromBB)
                {
                    var toBB = Bitboard
                        .LanceAttacks(pos.Player, from, occupancy)
                        .AndNot(us);
                    foreach (var to in toBB)
                    {
                        var rank = Square.RankOf(pos.Player, to);
                        if (rank == 0)
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, true));
                        }
                        else if (rank <= 2)
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, false));
                            moves.Add(MoveExtensions.MakeMove(from, to, true));
                        }
                        else
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, false));
                        }
                    }
                }
            }

            // 桂
            {
                var fromBB = pos.PieceBB(pos.Player, Piece.Knight).AndNot(pinned);
                foreach (var from in fromBB)
                {
                    var toBB = Bitboard
                        .KnightAttacks(pos.Player, from)
                        .AndNot(us);
                    foreach (var to in toBB)
                    {
                        var rank = Square.RankOf(pos.Player, to);
                        if (rank <= 1)
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, true));
                        }
                        else if (rank == 2)
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, false));
                            moves.Add(MoveExtensions.MakeMove(from, to, true));
                        }
                        else
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, false));
                        }
                    }
                }
            }

            // 銀、角、飛
            {
                var fromBB = (pos.PieceBB(pos.Player, Piece.Silver)
                    | pos.PieceBB(pos.Player, Piece.Bishop)
                    | pos.PieceBB(pos.Player, Piece.Rook)).AndNot(pinned);

                foreach (var from in fromBB)
                {
                    var toBB = Bitboard
                        .Attacks(pos.PieceAt(from), from, occupancy)
                        .AndNot(us);
                    foreach (var to in toBB)
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to, false));
                        if (Square.CanPromote(pos.Player, from, to))
                        {
                            moves.Add(MoveExtensions.MakeMove(from, to, true));
                        }
                    }
                }
            }

            // 玉
            {
                var from = pos.King(pos.Player);
                var toBB = Bitboard.KingAttacks(from).AndNot(us);
                foreach (var to in toBB)
                {
                    if (pos.EnumerateAttackers(
                        pos.Player.Opponent(), to).None())
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to));
                    }
                }
            }

            // その他
            {
                var fromBB = (pos.Golds(pos.Player) ^ pos.PieceBB(pos.Player, Piece.King))
                    .AndNot(pinned);
                foreach (var from in fromBB)
                {
                    var toBB = Bitboard
                        .Attacks(pos.PieceAt(from), from, occupancy)
                        .AndNot(us);
                    foreach (var to in toBB)
                    {
                        moves.Add(MoveExtensions.MakeMove(from, to));
                    }
                }
            }

            // ピンされている駒
            if (pinned.Any())
            {
                var ksq = pos.King(pos.Player);
                foreach (var from in pinned)
                {
                    var toBB = Bitboard
                        .Attacks(pos.PieceAt(from), from, occupancy)
                        .AndNot(us) & Bitboard.Line(ksq, from);
                    foreach (var to in toBB)
                    {
                        AddMovesToList(pos.PieceAt(from), from, to, moves);
                    }
                }
            }

            // 駒打ち
            GenerateDrops(pos, ~occupancy, moves);

            return moves;
        }

        private static List<Move> GenerateEvasionMoves(Position pos)
        {
            var moves = new List<Move>();
            var ksq = pos.King(pos.Player);
            var checkerCount = pos.Checkers().Popcount();

            var evasionTo = Bitboard.KingAttacks(ksq)
                .AndNot(pos.ColorBB(pos.Player));
            var occ = pos.GetOccupancy() ^ ksq;

            foreach (var to in evasionTo)
            {
                var canMove = pos.EnumerateAttackers(
                    pos.Player.Opponent(), to, occ)
                    .None();
                if (canMove)
                {
                    moves.Add(MoveExtensions.MakeMove(ksq, to));
                }
            }

            if (checkerCount > 1) return moves;

            var csq = pos.Checkers().LsbSquare();
            var between = Bitboard.Between(ksq, csq);

            // 駒打ち
            GenerateDrops(pos, between, moves);

            // 駒移動
            var excluded = pos.PieceBB(pos.Player, Piece.King) | pos.PinnedBy(pos.Player.Opponent());

            foreach (var to in between | pos.Checkers())
            {
                var fromBB = pos
                    .EnumerateAttackers(pos.Player, to)
                    .AndNot(excluded);
                foreach (var from in fromBB)
                {
                    AddMovesToList(pos.PieceAt(from), from, to, moves);
                }
            }

            return moves;
        }

        private static partial void GenerateDrops(Position pos, Bitboard target, List<Move> moves);

        [InlineBitboardEnumerator]
        private static void GenerateDropsImpl(Position pos, Bitboard target, List<Move> moves)
        {
            foreach (var p in PieceExtensions.PawnToRook)
            {
                if (pos.CaptureListOf(pos.Player).Count(p) > 0)
                {
                    var toBB = target
                        & Bitboard.ReachableMask(pos.Player, p);
                    if (p == Piece.Pawn)
                    {
                        toBB &= Bitboard.PawnDropMask(pos.PieceBB(pos.Player, Piece.Pawn));
                        var o = pos.Player.Opponent();
                        var uchifuzumeCand = Bitboard.PawnAttacks(o, pos.King(o));
                        if (!toBB.TestZ(uchifuzumeCand)
                            && IsUchifuzume(uchifuzumeCand.LsbSquare(), pos))
                        {
                            toBB ^= uchifuzumeCand;
                        }
                    }
                    foreach (var to in toBB)
                    {
                        moves.Add(MoveExtensions.MakeDrop(p, to));
                    }
                }
            }
        }

        private static void AddMovesToList(Piece p, int from, int to, List<Move> moves)
        {
            var c = p.Color();
            p = p.Colorless();

            if ((Square.RankOf(c, to) <= 1 && p == Piece.Knight)
                || (Square.RankOf(c, to) == 0 && (p == Piece.Pawn || p == Piece.Lance)))
            {
                moves.Add(MoveExtensions.MakeMove(from, to, true));
            }
            else
            {
                moves.Add(MoveExtensions.MakeMove(from, to, false));

                if (Square.CanPromote(c, from, to)
                    && !(p.IsPromoted() || p == Piece.Gold || p == Piece.King))
                    moves.Add(MoveExtensions.MakeMove(from, to, true));
            }
        }

        private static bool IsUchifuzume(int to, Position pos)
        {
            var theirKsq = pos.King(pos.Player.Opponent());
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
