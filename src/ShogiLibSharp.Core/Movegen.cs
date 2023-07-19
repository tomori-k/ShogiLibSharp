using ShogiLibSharp.MovegenGenerator;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ShogiLibSharp.Core;

public partial class Position
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
    /// 指し手バッファの長さ
    /// </summary>
    public const int BufferSize = 600;

    class ListView<T>
    {
        public T[]? _items;
        public int _size;
    }

    /// <summary>
    /// 合法手を生成する。
    /// </summary>
    public List<Move> GenerateMoves()
    {
        var moveList = new List<Move>(BufferSize);
        ref var moveListview = ref Unsafe.As<List<Move>, ListView<Move>>(ref moveList);

        unsafe
        {
            fixed (Move* start = moveListview._items)
            {
                var end = GenerateMoves(start);
                moveListview._size = (int)(end - start);
            }
        }

        return moveList;
    }

    /// <summary>
    /// 合法手を生成する。 <br/>
    /// </summary>
    /// <param name="buffer">長さ 600 以上のバッファの先頭ポインタ。</param>>
    /// <param name="pos">局面</param>
    /// <returns>生成した指し手列の終端。</returns>
    public unsafe partial Move* GenerateMoves(Move* buffer);

    [InlineBitboardEnumerator, MakePublic]
    private unsafe Move* GenerateMovesImpl(Move* buffer)
    {
        if (InCheck)
        {
            return GenerateEvasionMoves(buffer);
        }

        var occupancy = Occupancy;
        var us = this[Player];
        var pinned = this.PinnedBy(Player.Inv()) & us;

        // 歩
        {
            var fromBB = this[Player, Piece.Pawn].AndNot(pinned);
            var toBB = (Player == Color.Black ? fromBB >> 1 : fromBB << 1).AndNot(us);
            var delta = Player == Color.Black ? 1 : -1;

            foreach (var to in toBB)
            {
                var from = to + delta;
                var rank = to.Rank(Player);

                switch (rank)
                {
                    case Rank.R1:
                        *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        break;
                    case Rank.R2:
                    case Rank.R3:
                        *buffer++ = MoveExtensions.MakeMove(from, to, false);
                        *buffer++ = MoveExtensions.MakeMove(from, to, true);
                        break;
                    default:
                        *buffer++ = MoveExtensions.MakeMove(from, to, false);
                        break;
                }
            }
        }
        // 香
        {
            var fromBB = this[Player, Piece.Lance].AndNot(pinned);

            foreach (var from in fromBB)
            {
                var toBB = Bitboard
                    .LanceAttacks(Player, from, occupancy)
                    .AndNot(us);

                foreach (var to in toBB)
                {
                    var rank = to.Rank(Player);

                    switch (rank)
                    {
                        case Rank.R1:
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                            break;
                        case Rank.R2:
                        case Rank.R3:
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                            break;
                        default:
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            break;
                    }
                }
            }
        }
        // 桂
        {
            var fromBB = this[Player, Piece.Knight].AndNot(pinned);

            foreach (var from in fromBB)
            {
                var toBB = Bitboard
                    .KnightAttacks(Player, from)
                    .AndNot(us);

                foreach (var to in toBB)
                {
                    var rank = to.Rank(Player);

                    switch (rank)
                    {
                        case Rank.R1:
                        case Rank.R2:
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                            break;
                        case Rank.R3:
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            *buffer++ = MoveExtensions.MakeMove(from, to, true);
                            break;
                        default:
                            *buffer++ = MoveExtensions.MakeMove(from, to, false);
                            break;
                    }
                }
            }
        }

        // 銀、角、飛
        {
            var fromBB = (this[Player, Piece.Silver] | this[Player, Piece.Bishop] | this[Player, Piece.Rook]).AndNot(pinned);

            foreach (var from in fromBB)
            {
                var toBB = Bitboard
                    .Attacks(this[from], from, occupancy)
                    .AndNot(us);

                foreach (var to in toBB)
                {
                    *buffer++ = MoveExtensions.MakeMove(from, to, false);

                    if (Squares.CanPromote(Player, from, to))
                    {
                        *buffer++ = MoveExtensions.MakeMove(from, to, true);
                    }
                }
            }
        }
        // 玉
        {
            var from = this.King(Player);
            var toBB = Bitboard.KingAttacks(from).AndNot(us);

            foreach (var to in toBB)
            {
                if (this.EnumerateAttackers(Player.Inv(), to).None())
                {
                    *buffer++ = MoveExtensions.MakeMove(from, to);
                }
            }
        }
        // その他
        {
            var fromBB = (this.GoldBB(Player) ^ this[Player, Piece.King]).AndNot(pinned);

            foreach (var from in fromBB)
            {
                var toBB = Bitboard
                    .Attacks(this[from], from, occupancy)
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
            var ksq = this.King(Player);

            foreach (var from in pinned)
            {
                var toBB = Bitboard
                    .Attacks(this[from], from, occupancy)
                    .AndNot(us) & Bitboard.Line(ksq, from);

                foreach (var to in toBB)
                {
                    buffer = AddMovesToList(buffer, this[from], from, to);
                }
            }
        }

        // 駒打ち
        return GenerateDrops(buffer, ~occupancy);
    }

    unsafe Move* GenerateEvasionMoves(Move* buffer)
    {
        var ksq = this.King(Player);
        var checkerCount = Checkers.Popcount();

        var evasionTo = Bitboard.KingAttacks(ksq).AndNot(this[Player]);
        var occ = Occupancy ^ ksq;

        foreach (var to in evasionTo)
        {
            var canMove = this.EnumerateAttackers(Player.Inv(), to, occ).None();

            if (canMove)
            {
                *buffer++ = MoveExtensions.MakeMove(ksq, to);
            }
        }

        if (checkerCount > 1) return buffer;

        var csq = Checkers.LsbSquare();
        var between = Bitboard.Between(ksq, csq);

        // 駒打ち
        buffer = GenerateDrops(buffer, between);

        // 駒移動
        var excluded = this[Player, Piece.King] | this.PinnedBy(Player.Inv());

        foreach (var to in between | Checkers)
        {
            var fromBB = this
                .EnumerateAttackers(Player, to)
                .AndNot(excluded);

            foreach (var from in fromBB)
            {
                buffer = AddMovesToList(buffer, this[from], from, to);
            }
        }

        return buffer;
    }

    private unsafe partial Move* GenerateDrops(Move* buffer, Bitboard target);

    [InlineBitboardEnumerator]
    private unsafe Move* GenerateDropsImpl(Move* buffer, Bitboard target)
    {
        var hand = this.Hand(Player);

        if (hand.None())
            return buffer;

        if (hand.Count(Piece.Pawn) > 0)
        {
            var toBB = target
                & Bitboard.ReachableMask(Player, Piece.Pawn)
                & Bitboard.PawnDropMask(this[Player, Piece.Pawn]);

            // 打ち歩詰めならそのマスを除去する
            {
                var o = Player.Inv();
                var cand = Bitboard.PawnAttacks(o, this.King(o));

                if (!toBB.TestZ(cand) && IsUchifuzume(cand.LsbSquare()))
                {
                    toBB ^= cand;
                }
            }

            foreach (var to in toBB)
            {
                *buffer++ = MoveExtensions.MakeDrop(Piece.Pawn, to);
            }
        }

        if (!hand.ExceptPawn())
            return buffer;

        var tmpl = stackalloc Move[10];
        int n = 0, li = 0;

        if (hand.Count(Piece.Knight) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Knight, 0);
            ++li;
        }
        if (hand.Count(Piece.Lance) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Lance, 0);
        }
        int other = n;
        if (hand.Count(Piece.Silver) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Silver, 0);
        }
        if (hand.Count(Piece.Gold) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Gold, 0);
        }
        if (hand.Count(Piece.Bishop) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Bishop, 0);
        }
        if (hand.Count(Piece.Rook) > 0)
        {
            tmpl[n++] = MoveExtensions.MakeDrop(Piece.Rook, 0);
        }

        var to1 = target & Rank1BB[(int)Player];
        var to2 = target & Rank2BB[(int)Player];
        var rem = target & Rank39BB[(int)Player];

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

    static unsafe Move* AddMovesToList(Move* buffer, Piece p, Square from, Square to)
    {
        var c = p.Color();
        p = p.Colorless();

        // 桂馬の1,2段目への移動または、歩、香車の1段目への移動なら、
        // 成る指し手のみ生成
        if ((to.Rank(c) <= Rank.R2 && p == Piece.Knight) || (to.Rank(c) == Rank.R1 && (p == Piece.Pawn || p == Piece.Lance)))
        {
            *buffer++ = MoveExtensions.MakeMove(from, to, true);
        }
        else
        {
            *buffer++ = MoveExtensions.MakeMove(from, to, false);

            if (Squares.CanPromote(c, from, to)
                && !(p.IsPromoted() || p == Piece.Gold || p == Piece.King))
                *buffer++ = MoveExtensions.MakeMove(from, to, true);
        }

        return buffer;
    }

    static readonly Bitboard[] Rank1BB = new Bitboard[2];
    static readonly Bitboard[] Rank2BB = new Bitboard[2];
    static readonly Bitboard[] Rank39BB = new Bitboard[2];

    static Position()
    {
        Rank1BB[0] = Bitboard.Rank(Color.Black, Rank.R1, Rank.R1);
        Rank2BB[0] = Bitboard.Rank(Color.Black, Rank.R2, Rank.R2);
        Rank39BB[0] = Bitboard.Rank(Color.Black, Rank.R3, Rank.R9);
        Rank1BB[1] = Bitboard.Rank(Color.White, Rank.R1, Rank.R1);
        Rank2BB[1] = Bitboard.Rank(Color.White, Rank.R2, Rank.R2);
        Rank39BB[1] = Bitboard.Rank(Color.White, Rank.R3, Rank.R9);
    }
}
