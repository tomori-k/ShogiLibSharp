using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace ShogiLibSharp
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
    public struct Bitboard
    {
        Vector128<ulong> x;

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
            foreach (var (c, i)
                in bitPattern.Select((x, i) => (x, i)))
            {
                if (c != 'o') continue;
                var rank = i / 9;
                var file = 8 - i % 9;
                this |= Square.Index(rank, file);
            }
        }

        public static Bitboard operator&(Bitboard lhs, Bitboard rhs)
        {
            return new(Avx2.And(lhs.x, rhs.x));
        }

        public static Bitboard operator|(Bitboard lhs, Bitboard rhs)
        {
            return new(Avx2.Or(lhs.x, rhs.x));
        }

        public static Bitboard operator^(Bitboard lhs, Bitboard rhs)
        {
            return new(Avx2.Xor(lhs.x, rhs.x));
        }

        public static Bitboard operator-(Bitboard lhs, Bitboard rhs)
        {
            return new(Avx2.Subtract(lhs.x, rhs.x));
        }

        public static Bitboard operator~(Bitboard x)
        {
            return x ^ new Bitboard(0x7fffffffffffffffUL, 0x000000000003ffffUL);
        }

        public static Bitboard operator>>(Bitboard lhs, int shift)
        {
            return new(Avx2.ShiftRightLogical(lhs.x, (byte)shift));
        }

        public Bitboard AndNot(Bitboard rhs)
        {
            return new(Avx2.AndNot(rhs.x, this.x));
        }

        public ulong Lower()
        {
            return this.x.ToScalar();
        }

        public ulong Upper()
        {
            return this.x.GetUpper().ToScalar();
        }

        /// <summary>
        /// 立っているビットの数が 0 か
        /// </summary>
        /// <returns></returns>
        public bool None()
        {
            return Avx2.TestZ(this.x, this.x);
        }

        /// <summary>
        /// 立っているビットが存在するか
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return !None();
        }

        /// <summary>
        /// 立っているビットの数
        /// </summary>
        /// <returns></returns>
        public int Popcount()
        {
            return Popcount64(Lower()) + Popcount64(Upper());
        }

        /// <summary>
        /// LSB のビットが示すマスの番号
        /// this.None() のとき、結果は不定
        /// </summary>
        /// <returns></returns>
        public int LsbSquare()
        {
            return Lower() != 0UL ? Tzcnt64(Lower()) : Tzcnt64(Upper()) + 63;
        }

        /// <summary>
        /// 128 ビットのビット列とみてバイト反転したビットボードを作成
        /// </summary>
        /// <returns></returns>
        public Bitboard Bswap()
        {
            throw new NotImplementedException();
            //var shuffle = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            //return Avx2.Shuffle(this.x.AsSByte(), shuffle);
        }

        /// <summary>
        /// (this & x).None() か
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool TestZ(Bitboard x)
        {
            return Avx2.TestZ(this.x, x.x);
        }

        /// <summary>
        /// sq のマスのビットが立っているか
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public bool Test(int sq)
        {
            return !this.TestZ(SQUARE_BIT[sq]);
        }

        /// <summary>
        /// ビットが立っているマスを列挙
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> Serialize()
        {
            var x = Lower();
            while (x != 0UL)
            {
                yield return Tzcnt64(x);
                x &= x - 1UL;
            }

            x = Upper();
            while (x != 0UL)
            {
                yield return Tzcnt64(x) + 63;
                x &= x - 1UL;
            }
        }

        private static readonly Bitboard[,] REACHABLE_MASK = new Bitboard[8, 2];
        private static readonly Bitboard[]  SQUARE_BIT = new Bitboard[81];
        private static readonly Bitboard[,] PAWN_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] KNIGHT_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] SILVER_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] GOLD_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[]  KING_ATTACKS = new Bitboard[81];
        private static readonly Bitboard[,] RAY_BB = new Bitboard[81,8]; // LEFT, LEFTUP, UP, RIGHTUP, RIGHT, RIGHTDOWN, DOWN, LEFTDOWN

        private static readonly Vector256<ulong>[,] BishopMask = new Vector256<ulong>[81, 2];
        private static readonly Vector128<ulong>[,] RookMask = new Vector128<ulong>[81, 2];


        private static Bitboard SquareBit(int rank, int file)
        {
            return 0 <= rank && rank < 9 && 0 <= file && file < 9
                ? SQUARE_BIT[Square.Index(rank, file)] : default;
        }

        /// <summary>
        /// sq から d の方向へ伸ばしたビットボード（sq は含まない）
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Bitboard Ray(int sq, Direction d)
        {
            return RAY_BB[sq, (int)d];
        }

        /// <summary>
        /// ２マスの間（両端は含まない）ビットボード
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
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
            int to   = c == Color.Black ? t : 8 - f;
            ulong mul  = (1UL << (to - from + 1)) - 1UL;
            ulong low  = 0x0040201008040201UL * mul << from;
            ulong high = 0x0000000000000201UL * mul << from;
            return new(low, high);
        }

        /// <summary>
        /// c の種類 p の駒を動かせる範囲を表すビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Bitboard ReachableMask(Color c, Piece p)
        {
            return REACHABLE_MASK[(int)p, (int)c];
        }

        public static Bitboard operator&(Bitboard lhs, int sq)
        {
            return lhs & SQUARE_BIT[sq];
        }

        public static Bitboard operator|(Bitboard lhs, int sq)
        {
            return lhs | SQUARE_BIT[sq];
        }

        public static Bitboard operator^(Bitboard lhs, int sq)
        {
            return lhs ^ SQUARE_BIT[sq];
        }

        /// <summary>
        /// 利き計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard Attacks(Piece p, int sq, Bitboard occupancy)
        {
            switch (p.Colorless())
            {
                case Piece.Pawn:
                    return PAWN_ATTACKS[sq, (int)p.Color()];
                case Piece.Lance:
                    return LanceAttacks(p.Color(), sq, occupancy);
                case Piece.Knight:
                    return KNIGHT_ATTACKS[sq, (int)p.Color()];
                case Piece.Silver:
                    return SILVER_ATTACKS[sq, (int)p.Color()];
                case Piece.Gold:
                case Piece.ProPawn:
                case Piece.ProLance:
                case Piece.ProKnight:
                case Piece.ProSilver:
                    return GOLD_ATTACKS[sq, (int)p.Color()];
                case Piece.Bishop:
                    return BishopAttacks(sq, occupancy);
                case Piece.Rook:
                    return RookAttacks(sq, occupancy);
                case Piece.King:
                    return KING_ATTACKS[sq];
                case Piece.ProBishop:
                    return BishopAttacks(sq, occupancy) | KING_ATTACKS[sq];
                case Piece.ProRook:
                    return RookAttacks(sq, occupancy) | KING_ATTACKS[sq];
                default:
                    return new Bitboard();
            }
        }

        /// <summary>
        /// 利き計算
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="sq"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        public static Bitboard Attacks(Color c, Piece p, int sq, Bitboard occupancy)
        {
            return Attacks(p.Colored(c), sq, occupancy);
        }

        /// <summary>
        /// 歩を打てる場所を表すビットボードを計算
        /// </summary>
        /// <param name="pawns"></param>
        /// <returns></returns>
        public static Bitboard PawnDropMask(Bitboard pawns)
        {
            var left = new Bitboard(0x4020100804020100UL, 0x0000000000020100UL);
            var t = left - pawns;
            t = (t & left) >> 8;
            return left ^ (left - t);
        }

        public static Bitboard LanceAttacksBlack(int sq, Bitboard occupancy)
        {
            var mask = Ray(sq, Direction.Right);
            var masked = occupancy & mask;
            masked |= masked >> 1;
            masked |= masked >> 2;
            masked |= masked >> 4;
            masked >>= 1;
            return mask.AndNot(masked);
        }

        public static Bitboard LanceAttacksWhite(int sq, Bitboard occupancy)
        {
            var mask = new Bitboard(0x3fdfeff7fbfdfeffUL, 0x000000000001feffUL);
            var masked = mask.AndNot(occupancy);
            var t = new Bitboard(Avx2.Add(masked.x, PAWN_ATTACKS[sq, 1].x));
            return t ^ masked;
        }

        public static Bitboard LanceAttacks(Color c, int sq, Bitboard occupancy)
        {
            return c == Color.Black
                ? LanceAttacksBlack(sq, occupancy)
                : LanceAttacksWhite(sq, occupancy);
        }

        public static Bitboard BishopAttacks(int sq, Bitboard occupancy)
        {
            var shuffle = Vector256.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            var mask_lo = BishopMask[sq, 0];
            var mask_hi = BishopMask[sq, 1];
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
            return new(Avx2.Or(a2.GetLower(), a2.GetUpper()).AsUInt64());
        }

        public static Bitboard RookAttacks(int sq, Bitboard occupancy)
        {
            var shuffle = Vector128.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            var mask_lo = RookMask[sq, 0];
            var mask_hi = RookMask[sq, 1];
            var rocc = Avx2.Shuffle(occupancy.x.AsSByte(), shuffle);
            var lo = Avx2.UnpackLow(occupancy.x, rocc.AsUInt64());
            var hi = Avx2.UnpackHigh(occupancy.x, rocc.AsUInt64());
            lo = Avx2.And(lo, mask_lo);
            hi = Avx2.And(hi, mask_hi);
            var t0 = Avx2.Add(lo, Vector128.Create(0xffffffffffffffffUL));
            var t1 = Avx2.Add(hi, Avx2.CompareEqual(lo, Vector128<ulong>.Zero));
            t0 = Avx2.Xor(t0, lo);
            t1 = Avx2.Xor(t1, hi);
            t0 = Avx2.And(t0, mask_lo);
            t1 = Avx2.And(t1, mask_hi);
            var updown = Avx2.Shuffle(Avx2.UnpackHigh(t0, t1).AsSByte(), shuffle).AsUInt64();
            updown = Avx2.Or(updown, Avx2.UnpackLow(t0, t1));
            return LanceAttacksBlack(sq, occupancy) | LanceAttacksWhite(sq, occupancy) | new Bitboard(updown);
        }

        /// <summary>
        /// 立っているビットの数
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int Popcount64(ulong x)
        {
            ulong t = x - (x >> 1 & 0x5555555555555555UL);
            t = (t & 0x3333333333333333UL) + (t >> 2 & 0x3333333333333333UL);
            t = (t & 0x0f0f0f0f0f0f0f0fUL) + (t >> 4 & 0x0f0f0f0f0f0f0f0fUL);
            return (int)(t * 0x0101010101010101UL >> 56);
        }

        /// <summary>
        /// trailing zero count
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int Tzcnt64(ulong x)
        {
            return Popcount64(~x & (x - 1));
        }

        /// <summary>
        /// バイトスワップ
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static ulong Bswap64(ulong x)
        {
            ulong t = (x >> 32) | (x << 32);
            t = (t >> 16 & 0x0000ffff0000ffffUL) | ((t & 0x0000ffff0000ffffUL) << 16);
            t = (t >> 8  & 0x00ff00ff00ff00ffUL) | ((t & 0x00ff00ff00ff00ffUL) <<  8);
            return t;
        }

        static Bitboard()
        {
            for (int rank = 0; rank < 9; ++rank)
                for (int file = 0; file < 9; ++file) {
                    SQUARE_BIT[Square.Index(rank, file)] = Square.Index(rank, file) < 63
                        ? new(1UL << Square.Index(rank, file), 0UL)
                        : new(0UL, 1UL << (Square.Index(rank, file) - 63));
                }

            for (int sq = 0; sq < 81; ++sq)
            {
                var dr = new[] { 1, 1, 0, -1, -1, -1, 0, 1 };
                var df = new[] { 0, 1, 1, 1, 0, -1, -1, -1 };
                for (int d = 0; d < 8; ++d)
                {
                    var rank = Square.RankOf(sq);
                    var file = Square.FileOf(sq);
                    while (true)
                    {
                        rank += dr[d]; file += df[d];

                        if (!(0 <= rank && rank < 9 && 0 <= file && file < 9))
                            break;

                        RAY_BB[sq, d] |= Square.Index(rank, file);
                    }
                }
            }

            for (int rank = 0; rank < 9; ++rank)
                for (int file = 0; file < 9; ++file)
                {
                    PAWN_ATTACKS[Square.Index(rank, file), (int)Color.Black]
                        = SquareBit(rank - 1, file);
                    PAWN_ATTACKS[Square.Index(rank, file), (int)Color.White]
                        = SquareBit(rank + 1, file);
                    KNIGHT_ATTACKS[Square.Index(rank, file), (int)Color.Black]
                        = SquareBit(rank - 2, file - 1) | SquareBit(rank - 2, file + 1);
                    KNIGHT_ATTACKS[Square.Index(rank, file), (int)Color.White]
                        = SquareBit(rank + 2, file - 1) | SquareBit(rank + 2, file + 1);
                    SILVER_ATTACKS[Square.Index(rank, file), (int)Color.Black]
                        = SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file)
                        | SquareBit(rank - 1, file + 1)
                        | SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file + 1);
                    SILVER_ATTACKS[Square.Index(rank, file), (int)Color.White]
                        = SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file)
                        | SquareBit(rank + 1, file + 1)
                        | SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file + 1);
                    GOLD_ATTACKS[Square.Index(rank, file), (int)Color.Black]
                        = SquareBit(rank - 1, file - 1)
                        | SquareBit(rank - 1, file)
                        | SquareBit(rank - 1, file + 1)
                        | SquareBit(rank, file - 1)
                        | SquareBit(rank, file + 1)
                        | SquareBit(rank + 1, file);
                    GOLD_ATTACKS[Square.Index(rank, file), (int)Color.White]
                        = SquareBit(rank + 1, file - 1)
                        | SquareBit(rank + 1, file)
                        | SquareBit(rank + 1, file + 1)
                        | SquareBit(rank, file - 1)
                        | SquareBit(rank, file + 1)
                        | SquareBit(rank - 1, file);
                }

            for (int i = 0; i < 81; ++i)
                KING_ATTACKS[i] = SILVER_ATTACKS[i, 0] | GOLD_ATTACKS[i, 0];

            foreach (var p in PieceExtensions.PawnToRook)
            {
                foreach (Color c in new[] { Color.Black, Color.White})
                {
                    REACHABLE_MASK[(int)p, (int)c] =
                        p == Piece.Pawn || p == Piece.Lance ? Rank(c, 1, 8)
                      : p == Piece.Knight                  ? Rank(c, 2, 8)
                      :                                     Rank(c, 0, 8);
                }
            }

            for (int i = 0; i < 81; ++i)
            {
                var up = Ray(i, Direction.Up).x;
                var down = Ray(i, Direction.Down).x;
                var rightup = Ray(i, Direction.RightUp).x;
                var leftup = Ray(i, Direction.LeftUp).x;
                var rightdown = Ray(i, Direction.RightDown).x;
                var leftdown = Ray(i, Direction.LeftDown).x;

                var shuffle = Vector128.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

                // 予めバイト反転しておく
                rightdown = Avx2.Shuffle(rightdown.AsSByte(), shuffle).AsUInt64();
                leftdown = Avx2.Shuffle(leftdown.AsSByte(), shuffle).AsUInt64();
                down = Avx2.Shuffle(down.AsSByte(), shuffle).AsUInt64();

                BishopMask[i, 0] = Vector256.Create(rightup.GetLower().ToScalar(), leftdown.GetLower().ToScalar(), leftup.GetLower().ToScalar(), rightdown.GetLower().ToScalar());
                BishopMask[i, 1] = Vector256.Create(rightup.GetUpper().ToScalar(), leftdown.GetUpper().ToScalar(), leftup.GetUpper().ToScalar(), rightdown.GetUpper().ToScalar());
                RookMask[i, 0] = Vector128.Create(up.GetLower().ToScalar(), down.GetLower().ToScalar());
                RookMask[i, 1] = Vector128.Create(up.GetUpper().ToScalar(), down.GetUpper().ToScalar());
            }
        }

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
    }
}
