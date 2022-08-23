using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ShogiLibSharp.MovegenGenerator;

namespace ShogiLibSharp.Core
{
    public static partial class Movegen
    {
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

        /// <summary>
        /// 合法手を生成する。
        /// </summary>
        public static MoveList GenerateMoves(Position pos)
        {
            var buffer = new Move[BufferSize];
            unsafe
            {
                fixed (Move* start = buffer)
                {
                    var end = GenerateMoves(start, pos);
                    return new MoveList(buffer, (int)(end - start));
                }
            }
        }

        /// <summary>
        /// 指し手バッファの長さ
        /// </summary>
        public const int BufferSize = 600;

        /// <summary>
        /// 合法手を生成する。 <br/>
        /// </summary>
        /// <param name="buffer">長さ 600 以上のバッファの先頭ポインタ。</param>>
        /// <param name="pos">局面</param>
        /// <returns>生成した指し手列の終端。</returns>
        public static unsafe partial Move* GenerateMoves(Move* buffer, Position pos);

        [InlineBitboardEnumerator, MakePublic]
        private static unsafe Move* GenerateMovesImpl(Move* buffer, Position pos)
        {
            if (pos.InCheck()) return GenerateEvasionMoves(buffer, pos);

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
                        *buffer++ = MoveExtensions.MakeMove(from, to, true);
                    }
                    else if (rank <= 2)
                    {
                        *buffer++ = MoveExtensions.MakeMove(from, to, false);
                        *buffer++ = MoveExtensions.MakeMove(from, to, true);
                    }
                    else
                    {
                        *buffer++ = MoveExtensions.MakeMove(from, to, false);
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
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        }
                        else if (rank <= 2)
                        {
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        }
                        else
                        {
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
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
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        }
                        else if (rank == 2)
                        {
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        }
                        else
                        {
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
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
                        *buffer++ = MoveExtensions.MakeMove(from, to, false);
                        if (Square.CanPromote(pos.Player, from, to))
                        {
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
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
                        *buffer++ = MoveExtensions.MakeMove(from, to);
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
                        *buffer++ = MoveExtensions.MakeMove(from, to);
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
                        buffer = AddMovesToList(buffer, pos.PieceAt(from), from, to);
                    }
                }
            }

            // 駒打ち
            return GenerateDrops(buffer, pos, ~occupancy);
        }

        private static unsafe Move* GenerateEvasionMoves(Move* buffer, Position pos)
        {
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
                    *buffer++ = MoveExtensions.MakeMove(ksq, to);
                }
            }

            if (checkerCount > 1) return buffer;

            var csq = pos.Checkers().LsbSquare();
            var between = Bitboard.Between(ksq, csq);

            // 駒打ち
            buffer = GenerateDrops(buffer, pos, between);

            // 駒移動
            var excluded = pos.PieceBB(pos.Player, Piece.King) | pos.PinnedBy(pos.Player.Opponent());

            foreach (var to in between | pos.Checkers())
            {
                var fromBB = pos
                    .EnumerateAttackers(pos.Player, to)
                    .AndNot(excluded);
                foreach (var from in fromBB)
                {
                    buffer = AddMovesToList(buffer, pos.PieceAt(from), from, to);
                }
            }

            return buffer;
        }

        private static unsafe partial Move* GenerateDrops(Move* buffer, Position pos, Bitboard target);

        [InlineBitboardEnumerator]
        private static unsafe Move* GenerateDropsImpl(Move* buffer, Position pos, Bitboard target)
        {
            var captureList = pos.CaptureListOf(pos.Player);
            if (captureList.None()) return buffer;

            if (captureList.Count(Piece.Pawn) > 0)
            {
                var toBB = target
                    & Bitboard.ReachableMask(pos.Player, Piece.Pawn)
                    & Bitboard.PawnDropMask(pos.PieceBB(pos.Player, Piece.Pawn));
                {
                    var o = pos.Player.Opponent();
                    var uchifuzumeCand = Bitboard.PawnAttacks(o, pos.King(o));
                    if (!toBB.TestZ(uchifuzumeCand) && IsUchifuzume(uchifuzumeCand.LsbSquare(), pos))
                    {
                        toBB ^= uchifuzumeCand;
                    }
                }
                foreach (var to in toBB)
                {
                    *buffer++ = MoveExtensions.MakeDrop(Piece.Pawn, to);
                }
            }

            if (!captureList.ExceptPawn()) return buffer;

            var tmpl = stackalloc Move[10];
            int n = 0, li = 0;

            if (captureList.Count(Piece.Knight) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Knight, 0);
                ++li;
            }
            if (captureList.Count(Piece.Lance) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Lance, 0);
            }
            int other = n;
            if (captureList.Count(Piece.Silver) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Silver, 0);
            }
            if (captureList.Count(Piece.Gold) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Gold, 0);
            }
            if (captureList.Count(Piece.Bishop) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Bishop, 0);
            }
            if (captureList.Count(Piece.Rook) > 0)
            {
                tmpl[n++] = MoveExtensions.MakeDrop(Piece.Rook, 0);
            }

            var to1 = target & Bitboard.Rank(pos.Player, 0, 0);
            var to2 = target & Bitboard.Rank(pos.Player, 1, 1);
            var rem = target & Bitboard.Rank(pos.Player, 2, 8);

            if (Sse2.IsSupported)
            {
                if (n - other != 0)
                {
                    var tmpl8 = Sse2.LoadVector128((ushort*)(tmpl + other));
                    foreach (var to in to1)
                    {
                        var to8 = Vector128.Create((ushort)to);
                        Sse2.Store((ushort*)buffer, Sse2.Add(tmpl8, to8));
                        buffer += n - other;
                    }
                }
                if (n - li != 0)
                {
                    var tmpl8 = Sse2.LoadVector128((ushort*)(tmpl + li));
                    foreach (var to in to2)
                    {
                        var to8 = Vector128.Create((ushort)to);
                        Sse2.Store((ushort*)buffer, Sse2.Add(tmpl8, to8));
                        buffer += n - li;
                    }
                }
                // n != 0
                {
                    var tmpl8 = Sse2.LoadVector128((ushort*)tmpl);
                    foreach (var to in rem)
                    {
                        var to8 = Vector128.Create((ushort)to);
                        Sse2.Store((ushort*)buffer, Sse2.Add(tmpl8, to8));
                        buffer += n;
                    }
                }
            }
            else
            {
                // sizeof(nuint) == 4(32bit) or 8(64bit) のみ考える
                Debug.Assert(sizeof(nuint) == 4 || sizeof(nuint) == 8);

                if (n - other != 0)
                {
                    var tmpl_p = (nuint*)(tmpl + other);
                    foreach (var to in to1)
                    {
                        var multiTo = unchecked((nuint)0x0001000100010001UL) * (nuint)to;
                        var p = (nuint*)buffer;
                        *p = *tmpl_p + multiTo;
                        if (sizeof(nuint) == 4) *(p + 1) = *(tmpl_p + 1) * multiTo;
                        buffer += n - other;
                    }
                }
                if (n - li != 0)
                {
                    var tmpl_p = (nuint*)(tmpl + li);
                    foreach (var to in to2)
                    {
                        var multiTo = unchecked((nuint)0x0001000100010001UL) * (nuint)to;
                        var p = (nuint*)buffer;
                        *p = *tmpl_p + multiTo;
                        *(p + 1) = *(tmpl_p + 1) + multiTo;
                        if (sizeof(nuint) == 4) *(p + 2) = *(tmpl_p + 2) * multiTo;
                        buffer += n - li;
                    }
                }
                // n != 0
                {
                    var tmpl_p = (nuint*)tmpl;
                    foreach (var to in rem)
                    {
                        var multiTo = unchecked((nuint)0x0001000100010001UL) * (nuint)to;
                        var p = (nuint*)buffer;
                        *p = *tmpl_p + multiTo;
                        *(p + 1) = *(tmpl_p + 1) + multiTo;
                        if (sizeof(nuint) == 4) *(p + 2) = *(tmpl_p + 2) * multiTo;
                        buffer += n;
                    }
                }
            }

            return buffer;
        }

        static unsafe Move* AddMovesToList(Move* buffer, Piece p, int from, int to)
        {
            var c = p.Color();
            p = p.Colorless();

            if ((Square.RankOf(c, to) <= 1 && p == Piece.Knight)
                || (Square.RankOf(c, to) == 0 && (p == Piece.Pawn || p == Piece.Lance)))
            {
                *buffer++ = MoveExtensions.MakeMove(from, to, true);
            }
            else
            {
                *buffer++ = MoveExtensions.MakeMove(from, to, false);

                if (Square.CanPromote(c, from, to)
                    && !(p.IsPromoted() || p == Piece.Gold || p == Piece.King))
                    *buffer++ = MoveExtensions.MakeMove(from, to, true);
            }

            return buffer;
        }

        static bool IsUchifuzume(int to, Position pos)
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
