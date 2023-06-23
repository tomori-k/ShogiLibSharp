using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace ShogiLibSharp.Core
{
    /// <summary>
    /// 駒があるかないかのみを表すデータ構造                 <br/>
    /// ビットと盤面の対応                                 <br/>
    /// 9  8        7  6  5  4  3  2  1                   <br/>
    /// 09 00       54 45 36 27 18 09 00 一               <br/>
    /// 10 01       55 46 37 28 19 10 01 二               <br/>
    /// 11 02       56 47 38 29 20 11 02 三      ↑ RIGHT  <br/>
    /// 12 03       57 48 39 30 21 12 03 四 UP ←   → DOWN <br/>
    /// 13 04       58 49 40 31 22 13 04 五      ↓ LEFT   <br/>
    /// 14 05       59 50 41 32 23 14 05 六               <br/>
    /// 15 06       60 51 42 33 24 15 06 七               <br/>
    /// 16 07       61 52 43 34 25 16 07 八               <br/>
    /// 17 08       62 53 44 35 26 17 08 九               <br/>
    ///    hi                         lo                  <br/>
    /// </summary>
    public readonly struct Bitboard : IEnumerable<int>
    {
        #region テーブル

        static readonly Bitboard[] REACHABLE_MASK = new Bitboard[8 * 2];
        static readonly Bitboard[] SQUARE_BIT = new Bitboard[81];
        static readonly Bitboard[] PAWN_ATTACKS = new Bitboard[81 * 2];
        static readonly Bitboard[] KNIGHT_ATTACKS = new Bitboard[81 * 2];
        static readonly Bitboard[] SILVER_ATTACKS = new Bitboard[81 * 2];
        static readonly Bitboard[] GOLD_ATTACKS = new Bitboard[81 * 2];
        static readonly Bitboard[] KING_ATTACKS = new Bitboard[81];
        static readonly Bitboard[] LANCE_PSEUDO_ATTACKS = new Bitboard[81 * 2];
        static readonly Bitboard[] BISHOP_PSEUDO_ATTACKS = new Bitboard[81];
        static readonly Bitboard[] ROOK_PSEUDO_ATTACKS = new Bitboard[81];

        static readonly Bitboard[] RAY_BB = new Bitboard[81 * 8]; // LEFT, LEFTUP, UP, RIGHTUP, RIGHT, RIGHTDOWN, DOWN, LEFTDOWN

        static readonly Vector256<ulong>[] BishopMask = new Vector256<ulong>[81 * 2];
        static readonly Vector128<ulong>[] RookMask = new Vector128<ulong>[81 * 2];

        #endregion

        readonly Vector128<ulong> x;

        public Bitboard(ulong lo, ulong hi)
        {
            this.x = Vector128.Create(lo, hi);
        }

        public Bitboard(Vector128<UInt64> x)
        {
            this.x = x;
        }

        public Bitboard(string bitPattern)
        {
            this.x = Vector128<ulong>.Zero;

            foreach (var (c, i) in bitPattern.Select((x, i) => (x, i)))
            {
                if (c != 'o')
                    continue;

                var rank = (Rank)(i / 9);
                var file = (File)(8 - i % 9);

                this |= Squares.Index(rank, file);
            }
        }

        /// <summary>
        /// 指定したマスのビットが立った `Bitboard` を取得する。
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static Bitboard SquareBB(Square sq)
        {
            return SQUARE_BIT[(int)sq];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator &(Bitboard lhs, Bitboard rhs)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.And(lhs.x, rhs.x));
            }
            else
            {
                return new(lhs.Lower() & rhs.Lower(), lhs.Upper() & rhs.Upper());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator |(Bitboard lhs, Bitboard rhs)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.Or(lhs.x, rhs.x));
            }
            else
            {
                return new(lhs.Lower() | rhs.Lower(), lhs.Upper() | rhs.Upper());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator ^(Bitboard lhs, Bitboard rhs)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.Xor(lhs.x, rhs.x));
            }
            else
            {
                return new(lhs.Lower() ^ rhs.Lower(), lhs.Upper() ^ rhs.Upper());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator <<(Bitboard x, int shift)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.ShiftLeftLogical(x.x, (byte)shift));
            }
            else
            {
                return new(x.Lower() << shift, x.Upper() << shift);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator >>(Bitboard x, int shift)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.ShiftRightLogical(x.x, (byte)shift));
            }
            else
            {
                return new(x.Lower() >> shift, x.Upper() >> shift);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator &(Bitboard lhs, Square sq)
        {
            return lhs & SquareBB(sq);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator |(Bitboard lhs, Square sq)
        {
            return lhs | SquareBB(sq);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator ^(Bitboard lhs, Square sq)
        {
            return lhs ^ SquareBB(sq);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator ~(Bitboard x)
        {
            return x ^ new Bitboard(0x7fffffffffffffffUL, 0x000000000003ffffUL);
        }

        /// <summary>
        /// this &amp; ~rhs
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard AndNot(Bitboard rhs)
        {
            if (Sse2.IsSupported)
            {
                return new(Sse2.AndNot(rhs.x, this.x));
            }
            else
            {
                return this & ~rhs;
            }
        }

        /// <summary>
        /// 1 筋 から 7 筋までのビットボード
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Lower()
        {
            return this.x.ToScalar();
        }

        /// <summary>
        /// 8, 9 筋のビットボード
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Upper()
        {
            return this.x.GetUpper().ToScalar();
        }

        /// <summary>
        /// 立っているビットの数が 0 か
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool None()
        {
            if (Sse41.IsSupported)
            {
                return Sse41.TestZ(this.x, this.x);
            }
            else
            {
                return (Lower() | Upper()) == 0UL;
            }
        }

        /// <summary>
        /// 立っているビットが存在するか
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Any()
        {
            return !None();
        }

        /// <summary>
        /// 立っているビットの数
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Popcount()
        {
            return BitOperations.PopCount(Lower()) + BitOperations.PopCount(Upper());
        }

        /// <summary>
        /// LSB のビットが示すマスの番号
        /// this.None() のとき、結果は不定
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LsbSquare()
        {
            return Lower() != 0UL
                ? BitOperations.TrailingZeroCount(Lower())
                : BitOperations.TrailingZeroCount(Upper()) + 63;
        }

        /// <summary>
        /// (this &amp; x).None() か
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestZ(Bitboard x)
        {
            if (Sse41.IsSupported)
            {
                return Sse41.TestZ(this.x, x.x);
            }
            else
            {
                return (this & x).None();
            }
        }

        /// <summary>
        /// sq のマスのビットが立っているか
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Test(Square sq)
        {
            return !this.TestZ(SquareBB(sq));
        }

        public struct Enumerator : IEnumerator<int>
        {
            bool first = true;
            ulong b0, b1;

            internal Enumerator(Bitboard x)
            {
                this.b0 = x.Lower();
                this.b1 = x.Upper();
            }

            public int Current
                => b0 != 0UL
                    ? BitOperations.TrailingZeroCount(b0)
                    : BitOperations.TrailingZeroCount(b1) + 63;

            object System.Collections.IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                if (first)
                {
                    first = false;
                    return b0 != 0UL || b1 != 0UL;
                }
                else
                {
                    if (b0 != 0UL)
                    {
                        b0 &= b0 - 1UL;
                        return b0 != 0UL || b1 != 0UL;
                    }
                    else if (b1 != 0UL)
                    {
                        b1 &= b1 - 1UL;
                        return b1 != 0UL;
                    }
                    else
                        return false;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// sq から d の方向へ伸ばしたビットボード（sq は含まない）
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard Ray(Square sq, Direction d)
        {
            return RAY_BB[(int)sq * 8 + (int)d];
        }

        /// <summary>
        /// ２マスの間（両端は含まない）ビットボード
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard Between(int i, int j)
        {
            Direction d = DirectionExtensions.FromTo(i, j);
            return d != Direction.None
                ? Ray(i, d) & Ray(j, d.Reverse()) : default;
        }

        /// <summary>
        /// ２マスを通る直線のビットボード
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard Line(int i, int j)
        {
            Direction d = DirectionExtensions.FromTo(i, j);
            return d != Direction.None
                ? Ray(i, d) | Ray(j, d.Reverse()) : default;
        }

        /// <summary>
        /// c 視点で、段 f から 段 t までを表すビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <param name="f"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Bitboard Rank(Color c, int f, int t)
        {
            int from = c == Color.Black ? f : 8 - t;
            int to = c == Color.Black ? t : 8 - f;
            ulong mul = (1UL << (to - from + 1)) - 1UL;
            ulong low = 0x0040201008040201UL * mul << from;
            ulong high = 0x0000000000000201UL * mul << from;
            return new(low, high);
        }

        /// <summary>
        /// c の種類 p の駒を動かせる範囲を表すビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ReachableMask(Color c, Piece p)
        {
            return REACHABLE_MASK[(int)p * 2 + (int)c];
        }

        /// <summary>
        /// 歩を打てる場所を表すビットボードを計算
        /// </summary>
        /// <param name="pawns"></param>
        /// <returns></returns>
        public static Bitboard PawnDropMask(Bitboard pawns)
        {
            if (Sse2.IsSupported)
            {
                var left = Vector128.Create(0x4020100804020100UL, 0x0000000000020100UL);
                var t = Sse2.Subtract(left, pawns.x);
                t = Sse2.ShiftRightLogical(Sse2.And(t, left), 8);
                return new(Sse2.Xor(left, Sse2.Subtract(left, t)));
            }
            else
            {
                const ulong left0 = 0x4020100804020100UL;
                const ulong left1 = 0x0000000000020100UL;
                var t0 = left0 - pawns.Lower();
                var t1 = left1 - pawns.Upper();
                t0 = left0 - ((t0 & left0) >> 8);
                t1 = left1 - ((t1 & left1) >> 8);
                return new(left0 ^ t0, left1 ^ t1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int TableIndex(Color c, Square sq) => (int)sq * 2 + (int)c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard PawnAttacks(Color c, Square sq)
        {
            return PAWN_ATTACKS[TableIndex(c, sq)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard KnightAttacks(Color c, Square sq)
        {
            return KNIGHT_ATTACKS[TableIndex(c, sq)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard SilverAttacks(Color c, Square sq)
        {
            return SILVER_ATTACKS[TableIndex(c, sq)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard GoldAttacks(Color c, Square sq)
        {
            return GOLD_ATTACKS[TableIndex(c, sq)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard KingAttacks(Square sq)
        {
            return KING_ATTACKS[(int)sq];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard LancePseudoAttacks(Color c, Square sq)
        {
            return LANCE_PSEUDO_ATTACKS[TableIndex(c, sq)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard BishopPseudoAttacks(Square sq)
        {
            return BISHOP_PSEUDO_ATTACKS[(int)sq];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard RookPseudoAttacks(Square sq)
        {
            return ROOK_PSEUDO_ATTACKS[(int)sq];
        }

        /// <summary>
        /// 先手の香車の利きを計算
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard LanceAttacksBlack(Square sq, Bitboard occupancy)
        {
            if (Sse2.IsSupported)
            {
                var mask = Ray(sq, Direction.Right).x;
                var masked = Sse2.And(occupancy.x, mask);
                masked = Sse2.Or(masked, Sse2.ShiftRightLogical(masked, 1));
                masked = Sse2.Or(masked, Sse2.ShiftRightLogical(masked, 2));
                masked = Sse2.Or(masked, Sse2.ShiftRightLogical(masked, 4));
                masked = Sse2.ShiftRightLogical(masked, 1);
                return new(Sse2.AndNot(masked, mask));
            }
            else
            {
                var mask = sq < 63
                    ? Ray(sq, Direction.Right).Lower()
                    : Ray(sq, Direction.Right).Upper();
                var occ = sq < 63
                    ? occupancy.Lower() : occupancy.Upper();
                var masked = occ & mask;
                masked |= masked >> 1;
                masked |= masked >> 2;
                masked |= masked >> 4;
                masked >>= 1;
                return sq < 63
                    ? new(~masked & mask, 0UL)
                    : new(0UL, ~masked & mask);
            }
        }

        /// <summary>
        /// 後手の香車の利きを計算
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard LanceAttacksWhite(Square sq, Bitboard occupancy)
        {
            if (Sse2.IsSupported)
            {
                var mask = Ray(sq, Direction.Left);
                var minusOne = Vector128.Create(0xffffffffffffffffUL);
                var masked = Sse2.And(occupancy.x, mask.x);
                var t = Sse2.Add(masked, minusOne);
                return new(Sse2.And(Sse2.Xor(t, masked), mask.x));
            }
            else
            {
                var mask = sq < 63 ? Ray(sq, Direction.Left).Lower() : Ray(sq, Direction.Left).Upper();
                var occ = sq < 63 ? occupancy.Lower() : occupancy.Upper();
                var masked = occ & mask;
                var a = (masked ^ (masked - 1)) & mask;
                return sq < 63 ? new(a, 0UL) : new(0UL, a);
            }
        }

        /// <summary>
        /// 香車の利きを計算
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard LanceAttacks(Color c, Square sq, Bitboard occupancy)
        {
            return c == Color.Black
                ? LanceAttacksBlack(sq, occupancy)
                : LanceAttacksWhite(sq, occupancy);
        }

        /// <summary>
        /// 角の利きを計算
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard BishopAttacks(Square sq, Bitboard occupancy)
        {
            if (Avx2.IsSupported)
            {
                var shuffle = Vector256.Create(
                    15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                    15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
                var mask_lo = BishopMask[(int)sq * 2 + 0];
                var mask_hi = BishopMask[(int)sq * 2 + 1];
                var occ256 = occupancy.x.ToVector256Unsafe();
                var occ2 = Avx2.Permute2x128(occ256, occ256, 0x20);
                var rocc2 = Avx2.Shuffle(occ2.AsSByte(), shuffle).AsUInt64();
                var lo = Avx2.UnpackLow(occ2, rocc2);
                var hi = Avx2.UnpackHigh(occ2, rocc2);
                lo = Avx2.And(lo, mask_lo);
                hi = Avx2.And(hi, mask_hi);
                var t0 = Avx2.Add(lo, Vector256.Create(0xffffffffffffffffUL));
                var t1 = Avx2.Add(hi, Avx2.CompareEqual(lo, Vector256<ulong>.Zero));
                t0 = Avx2.And(Avx2.Xor(t0, lo), mask_lo);
                t1 = Avx2.And(Avx2.Xor(t1, hi), mask_hi);
                var a2 = Avx2.Shuffle(Avx2.UnpackHigh(t0, t1).AsSByte(), shuffle);
                a2 = Avx2.Or(a2, Avx2.UnpackLow(t0, t1).AsSByte());
                return new(Sse2.Or(a2.GetLower(), a2.GetUpper()).AsUInt64());
            }
            else if (Sse2.IsSupported)
            {
                var shuffle = Vector128.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
                var rocc = Ssse3.IsSupported
                    ? Ssse3.Shuffle(occupancy.x.AsSByte(), shuffle).AsUInt64()
                    : Bswap128_Sse2(occupancy.x);
                var occ0 = Sse2.UnpackLow(occupancy.x, rocc);
                var occ1 = Sse2.UnpackHigh(occupancy.x, rocc);
                Vector128<ulong> res0, res1;
                // 前半
                {
                    var mask_lo = BishopMask[(int)sq * 2 + 0].GetLower();
                    var mask_hi = BishopMask[(int)sq * 2 + 1].GetLower();
                    var lo = Sse2.And(occ0, mask_lo);
                    var hi = Sse2.And(occ1, mask_hi);
                    var carry = Sse41.IsSupported
                        ? Sse41.CompareEqual(lo, Vector128<ulong>.Zero)
                        : AllBitsOneIfZero64x2_Sse2(lo);
                    var t1 = Sse2.Add(hi, carry);
                    var t0 = Sse2.Add(lo, Vector128.Create(0xffffffffffffffffUL));
                    t0 = Sse2.And(Sse2.Xor(t0, lo), mask_lo);
                    t1 = Sse2.And(Sse2.Xor(t1, hi), mask_hi);
                    var a = Ssse3.IsSupported
                        ? Ssse3.Shuffle(Sse2.UnpackHigh(t0, t1).AsSByte(), shuffle).AsUInt64()
                        : Bswap128_Sse2(Sse2.UnpackHigh(t0, t1));
                    res0 = Sse2.Or(a, Sse2.UnpackLow(t0, t1));
                }
                // 後半
                {
                    var mask_lo = BishopMask[(int)sq * 2 + 0].GetUpper();
                    var mask_hi = BishopMask[(int)sq * 2 + 1].GetUpper();
                    var lo = Sse2.And(occ0, mask_lo);
                    var hi = Sse2.And(occ1, mask_hi);
                    var carry = Sse41.IsSupported
                        ? Sse41.CompareEqual(lo, Vector128<ulong>.Zero)
                        : AllBitsOneIfZero64x2_Sse2(lo);
                    var t1 = Sse2.Add(hi, carry);
                    var t0 = Sse2.Add(lo, Vector128.Create(0xffffffffffffffffUL));
                    t0 = Sse2.And(Sse2.Xor(t0, lo), mask_lo);
                    t1 = Sse2.And(Sse2.Xor(t1, hi), mask_hi);
                    var a = Ssse3.IsSupported
                        ? Ssse3.Shuffle(Sse2.UnpackHigh(t0, t1).AsSByte(), shuffle).AsUInt64()
                        : Bswap128_Sse2(Sse2.UnpackHigh(t0, t1));
                    res1 = Sse2.Or(a, Sse2.UnpackLow(t0, t1));
                }
                return new(Sse2.Or(res0, res1));
            }
            else
            {
                return SliderAttacks_NoSse_LsbToMsb(sq, Direction.LeftUp, occupancy)
                    | SliderAttacks_NoSse_LsbToMsb(sq, Direction.RightUp, occupancy)
                    | SliderAttacks_NoSse_MsbToLsb(sq, Direction.LeftDown, occupancy)
                    | SliderAttacks_NoSse_MsbToLsb(sq, Direction.RightDown, occupancy);
            }
        }

        /// <summary>
        /// 飛車の利きを計算
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard RookAttacks(Square sq, Bitboard occupancy)
        {
            if (Sse2.IsSupported)
            {
                var shuffle = Vector128.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
                var mask_lo = RookMask[(int)sq * 2 + 0];
                var mask_hi = RookMask[(int)sq * 2 + 1];
                var rocc = Ssse3.IsSupported
                    ? Ssse3.Shuffle(occupancy.x.AsSByte(), shuffle).AsUInt64()
                    : Bswap128_Sse2(occupancy.x);
                var lo = Sse2.UnpackLow(occupancy.x, rocc);
                var hi = Sse2.UnpackHigh(occupancy.x, rocc);
                lo = Sse2.And(lo, mask_lo);
                hi = Sse2.And(hi, mask_hi);
                var carry = Sse41.IsSupported
                    ? Sse41.CompareEqual(lo, Vector128<ulong>.Zero)
                    : AllBitsOneIfZero64x2_Sse2(lo);
                var t1 = Sse2.Add(hi, carry);
                var t0 = Sse2.Add(lo, Vector128.Create(0xffffffffffffffffUL));
                t0 = Sse2.Xor(t0, lo);
                t1 = Sse2.Xor(t1, hi);
                t0 = Sse2.And(t0, mask_lo);
                t1 = Sse2.And(t1, mask_hi);
                var updown = Ssse3.IsSupported
                    ? Ssse3.Shuffle(Sse2.UnpackHigh(t0, t1).AsSByte(), shuffle).AsUInt64()
                    : Bswap128_Sse2(Sse2.UnpackHigh(t0, t1));
                updown = Sse2.Or(updown, Sse2.UnpackLow(t0, t1));
                return LanceAttacksBlack(sq, occupancy) | LanceAttacksWhite(sq, occupancy) | new Bitboard(updown);
            }
            else
            {
                return SliderAttacks_NoSse_LsbToMsb(sq, Direction.Up, occupancy)
                    | SliderAttacks_NoSse_MsbToLsb(sq, Direction.Down, occupancy)
                    | LanceAttacksBlack(sq, occupancy)
                    | LanceAttacksWhite(sq, occupancy);
            }
        }

        /// <summary>
        /// 利き計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard Attacks(Piece p, Square sq, Bitboard occupancy)
        {
            return p switch
            {
                Piece.B_Pawn => PawnAttacks(Color.Black, sq),
                Piece.B_Lance => LanceAttacksBlack(sq, occupancy),
                Piece.B_Knight => PawnAttacks(Color.Black, sq),
                Piece.B_Silver => SilverAttacks(Color.Black, sq),
                Piece.B_Gold => GoldAttacks(Color.Black, sq),
                Piece.B_Bishop => BishopAttacks(sq, occupancy),
                Piece.B_Rook => RookAttacks(sq, occupancy),
                Piece.B_King => KingAttacks(sq),
                Piece.B_ProPawn or Piece.B_ProLance or Piece.B_ProKnight or Piece.B_ProSilver => GoldAttacks(Color.Black, sq),
                Piece.B_ProBishop => BishopAttacks(sq, occupancy) | KingAttacks(sq),
                Piece.B_ProRook => RookAttacks(sq, occupancy) | KingAttacks(sq),
                Piece.W_Pawn => PawnAttacks(Color.White, sq),
                Piece.W_Lance => LanceAttacksWhite(sq, occupancy),
                Piece.W_Knight => KnightAttacks(Color.White, sq),
                Piece.W_Silver => SilverAttacks(Color.White, sq),
                Piece.W_Gold => GoldAttacks(Color.White, sq),
                Piece.W_Bishop => BishopAttacks(sq, occupancy),
                Piece.W_Rook => RookAttacks(sq, occupancy),
                Piece.W_King => KingAttacks(sq),
                Piece.W_ProPawn or Piece.W_ProLance or Piece.W_ProKnight or Piece.W_ProSilver => GoldAttacks(Color.White, sq),
                Piece.W_ProBishop => BishopAttacks(sq, occupancy) | KingAttacks(sq),
                Piece.W_ProRook => RookAttacks(sq, occupancy) | KingAttacks(sq),
                _ => new Bitboard(),
            };
        }

        /// <summary>
        /// 利き計算
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard Attacks(Color c, Piece p, Square sq, Bitboard occupancy)
        {
            return Attacks(p.Colored(c), sq, occupancy);
        }

        static Bitboard SquareBit(int rank, int file)
        {
            return 0 <= rank && rank < 9 && 0 <= file && file < 9
                ? SquareBB(Squares.Index((Rank)rank, (File)file)) : default;
        }

        static ulong Bswap64(ulong x)
        {
            ulong t = (x >> 32) | (x << 32);
            t = (t >> 16 & 0x0000ffff0000ffffUL) | ((t & 0x0000ffff0000ffffUL) << 16);
            t = (t >> 8 & 0x00ff00ff00ff00ffUL) | ((t & 0x00ff00ff00ff00ffUL) << 8);
            return t;
        }

        static Bitboard SliderAttacks_NoSse_LsbToMsb(Square sq, Direction d, Bitboard occupancy)
        {
            var mask0 = Ray(sq, d).Lower();
            var mask1 = Ray(sq, d).Upper();
            var occ0 = occupancy.Lower();
            var occ1 = occupancy.Upper();
            var masked0 = occ0 & mask0;
            var masked1 = occ1 & mask1;
            var t0 = masked0 - 1UL;
            var t1 = masked1 - Convert.ToUInt64(masked0 == 0);
            t0 ^= masked0;
            t1 ^= masked1;
            t0 &= mask0;
            t1 &= mask1;
            return new Bitboard(t0, t1);
        }

        static Bitboard SliderAttacks_NoSse_MsbToLsb(Square sq, Direction d, Bitboard occupancy)
        {
            var mask0 = Ray(sq, d).Lower();
            var mask1 = Ray(sq, d).Upper();
            var occ0 = occupancy.Lower();
            var occ1 = occupancy.Upper();
            var masked0 = Bswap64(occ1 & mask1);
            var masked1 = Bswap64(occ0 & mask0);
            var t0 = masked0 - 1UL;
            var t1 = masked1 - Convert.ToUInt64(masked0 == 0);
            (t0, t1) = (Bswap64(t1 ^ masked1), Bswap64(t0 ^ masked0));
            t0 &= mask0;
            t1 &= mask1;
            return new Bitboard(t0, t1);
        }

        static Vector128<ulong> Bswap128_Sse2(Vector128<ulong> x)
        {
            var y = Sse2.ShuffleLow(x.AsUInt16(), 0b00011011);
            y = Sse2.ShuffleHigh(y, 0b00011011);
            y = Sse2.Or(Sse2.ShiftRightLogical(y, 8), Sse2.ShiftLeftLogical(y, 8));
            return Sse2.Shuffle(y.AsUInt32(), 0b01001110).AsUInt64();
        }

        static Vector128<ulong> Bswap128_NoSse(Vector128<ulong> x)
        {
            return Vector128.Create(
                Bswap64(x.GetUpper().ToScalar()),
                Bswap64(x.GetLower().ToScalar()));
        }

        static Vector128<ulong> AllBitsOneIfZero64x2_Sse2(Vector128<ulong> left)
        {
            var x = Sse2.CompareEqual(left.AsUInt32(), Vector128<uint>.Zero).AsUInt64();
            return Sse2.And(x, Sse2.Or(Sse2.ShiftRightLogical(x, 32), Sse2.ShiftLeftLogical(x, 32)));
        }

        /// <summary>
        /// 人が読みやすい文字列に変換
        /// </summary>
        /// <returns></returns>
        public string Pretty()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １");

            for (int rank = 0; rank < 9; ++rank)
            {
                for (int file = 8; file >= 0; --file)
                {
                    sb.Append(
                        this.Test(Square.Index(rank, file)) ? " ◯" : "   ");
                }
                sb.AppendLine(Square.PrettyRank(rank));
            }

            return sb.ToString();
        }

        static Bitboard()
        {
            foreach (var rank in Ranks.All)
            {
                foreach (var file in Files.All)
                {
                    var sq = (int)Squares.Index(rank, file);

                    SQUARE_BIT[sq] = sq < 63
                        ? new(1UL << sq, 0UL)
                        : new(0UL, 1UL << (sq - 63));
                }
            }

            foreach (var sq in Squares.All)
            {
                var dr = new[] { 1, 1, 0, -1, -1, -1, 0, 1 };
                var df = new[] { 0, 1, 1, 1, 0, -1, -1, -1 };

                for (int d = 0; d < 8; ++d)
                {
                    var rank = sq.Rank();
                    var file = sq.File();

                    while (true)
                    {
                        rank += dr[d]; file += df[d];

                        if (!(Core.Rank.R1 <= rank && rank <= Core.Rank.R9 && Core.File.F1 <= file && file <= Core.File.F9))
                            break;

                        RAY_BB[(int)sq * 8 + d] |= Squares.Index(rank, file);
                    }
                }
            }

            foreach (int rank in Ranks.All)
            {
                foreach (int file in Files.All)
                {
                    var sq = (int)Squares.Index((Rank)rank, (File)file);

                    PAWN_ATTACKS[sq * 2 + 0]
                        = SquareBit(rank - 1, file);

                    PAWN_ATTACKS[sq * 2 + 1]
                        = SquareBit(rank + 1, file);

                    KNIGHT_ATTACKS[sq * 2 + 0]
                        = SquareBit(rank - 2, file - 1)
                        | SquareBit(rank - 2, file + 1);

                    KNIGHT_ATTACKS[sq * 2 + 1]
                        = SquareBit(rank + 2, file - 1)
                        | SquareBit(rank + 2, file + 1);

                    SILVER_ATTACKS[sq * 2 + 0]
                        = SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file)
                        | SquareBit(rank - 1, file + 1)
                        | SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file + 1);

                    SILVER_ATTACKS[sq * 2 + 1]
                        = SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file)
                        | SquareBit(rank + 1, file + 1)
                        | SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file + 1);

                    GOLD_ATTACKS[sq * 2 + 0]
                        = SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file)
                        | SquareBit(rank - 1, file + 1)
                        | SquareBit(rank, file - 1)
                        | SquareBit(rank, file + 1)
                        | SquareBit(rank + 1, file);

                    GOLD_ATTACKS[sq * 2 + 1]
                        = SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file)
                        | SquareBit(rank + 1, file + 1)
                        | SquareBit(rank, file - 1)
                        | SquareBit(rank, file + 1)
                        | SquareBit(rank - 1, file);
                }
            }

            for (int i = 0; i < 81; ++i)
            {
                KING_ATTACKS[i] = SILVER_ATTACKS[i * 2] | GOLD_ATTACKS[i * 2];
            }

            foreach (var p in Pieces.PawnToRook)
            {
                foreach (var c in Colors.All)
                {
                    REACHABLE_MASK[(int)p * 2 + (int)c] =
                        p == Piece.Pawn || p == Piece.Lance ? Rank(c, 1, 8)
                      : p == Piece.Knight ? Rank(c, 2, 8)
                      : Rank(c, 0, 8);
                }
            }

            for (int i = 0; i < 81; ++i)
            {
                var up = Ray(i, Direction.Up).x;
                var dn = Ray(i, Direction.Down).x;
                var ru = Ray(i, Direction.RightUp).x;
                var lu = Ray(i, Direction.LeftUp).x;
                var rd = Ray(i, Direction.RightDown).x;
                var ld = Ray(i, Direction.LeftDown).x;

                // 予めバイト反転しておく
                rd = Bswap128_NoSse(rd);
                ld = Bswap128_NoSse(ld);
                dn = Bswap128_NoSse(dn);

                BishopMask[i * 2 + 0] = Vector256.Create(ru.GetLower().ToScalar(), ld.GetLower().ToScalar(), lu.GetLower().ToScalar(), rd.GetLower().ToScalar());
                BishopMask[i * 2 + 1] = Vector256.Create(ru.GetUpper().ToScalar(), ld.GetUpper().ToScalar(), lu.GetUpper().ToScalar(), rd.GetUpper().ToScalar());
                RookMask[i * 2 + 0] = Vector128.Create(up.GetLower().ToScalar(), dn.GetLower().ToScalar());
                RookMask[i * 2 + 1] = Vector128.Create(up.GetUpper().ToScalar(), dn.GetUpper().ToScalar());
            }

            for (int i = 0; i < 81; ++i)
            {
                LANCE_PSEUDO_ATTACKS[i * 2 + 0] = LanceAttacksBlack(i, default);
                LANCE_PSEUDO_ATTACKS[i * 2 + 1] = LanceAttacksWhite(i, default);
                BISHOP_PSEUDO_ATTACKS[i] = BishopAttacks(i, default);
                ROOK_PSEUDO_ATTACKS[i] = RookAttacks(i, default);
            }
        }
    }
}
