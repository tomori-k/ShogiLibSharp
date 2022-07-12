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
    public struct Bitboard : IEnumerable<int>
    {
        private readonly ulong lo, hi;

        public Bitboard(ulong lo, ulong hi)
        {
            this.lo = lo;
            this.hi = hi;
        }

        public Bitboard(string bitPattern)
        {
            (this.lo, this.hi) = (0UL, 0UL);
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
            return new Bitboard(lhs.lo & rhs.lo, lhs.hi & rhs.hi);
        }

        public static Bitboard operator|(Bitboard lhs, Bitboard rhs)
        {
            return new Bitboard(lhs.lo | rhs.lo, lhs.hi | rhs.hi);
        }

        public static Bitboard operator^(Bitboard lhs, Bitboard rhs)
        {
            return new Bitboard(lhs.lo ^ rhs.lo, lhs.hi ^ rhs.hi);
        }

        public static Bitboard operator-(Bitboard lhs, Bitboard rhs)
        {
            return new Bitboard(lhs.lo - rhs.lo, lhs.hi - rhs.hi);
        }

        public static Bitboard operator~(Bitboard x)
        {
            return new Bitboard(x.lo ^ 0x7fffffffffffffffUL, x.hi ^ 0x000000000003ffffUL);
        }

        public static Bitboard operator>>(Bitboard lhs, int shift)
        {
            return new Bitboard(lhs.lo >> shift, lhs.hi >> shift);
        }

        /// <summary>
        /// 立っているビットの数が 0 か
        /// </summary>
        /// <returns></returns>
        public bool None()
        {
            return lo == 0UL && hi == 0UL;
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
            return Popcount64(lo) + Popcount64(hi);
        }

        /// <summary>
        /// LSB のビットが示すマスの番号
        /// this.None() のとき、結果は不定
        /// </summary>
        /// <returns></returns>
        public int LsbSquare()
        {
            return lo != 0UL ? Tzcnt64(lo) : Tzcnt64(hi) + 63;
        }

        /// <summary>
        /// 128 ビットのビット列とみてバイト反転したビットボードを作成
        /// </summary>
        /// <returns></returns>
        public Bitboard Bswap()
        {
            return new Bitboard(Bswap64(hi), Bswap64(lo));
        }

        /// <summary>
        /// (this & x).None() か
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool TestZ(Bitboard x)
        {
            return (this & x).None();
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
        public IEnumerator<int> GetEnumerator()
        {
            var x = lo;
            while (x != 0UL)
            {
                yield return Tzcnt64(x);
                x &= x - 1UL;
            }

            x = hi;
            while (x != 0UL)
            {
                yield return Tzcnt64(x) + 63;
                x &= x - 1UL;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static readonly Bitboard[,] REACHABLE_MASK = new Bitboard[8, 2];
        private static readonly Bitboard[]  SQUARE_BIT = new Bitboard[81];
        private static readonly Bitboard[,] PAWN_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] KNIGHT_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] SILVER_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[,] GOLD_ATTACKS = new Bitboard[81,2];
        private static readonly Bitboard[]  KING_ATTACKS = new Bitboard[81];
        private static readonly Bitboard[,] RAY_BB = new Bitboard[81,8]; // LEFT, LEFTUP, UP, RIGHTUP, RIGHT, RIGHTDOWN, DOWN, LEFTDOWN 

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
            Bitboard left = new(0x4020100804020100UL, 0x0000000000020100UL);
            Bitboard t = left - pawns;
            t = (t & left) >> 8;
            return left ^ (left - t);
        }

        private static Bitboard SliderAttacks(Direction d, int sq, Bitboard occupancy)
        {
            if (d != Direction.Right)
            {
                Bitboard m = Ray(sq, d);
                Bitboard t = occupancy & m;

                if (d.HasFlag(Direction.ReverseBit))
                {
                    t = t.Bswap();
                }

                t ^= new Bitboard(t.lo - 1UL, t.hi - Convert.ToUInt64(t.lo == 0UL));

                if (d.HasFlag(Direction.ReverseBit))
                {
                    t = t.Bswap();
                }

                return t & m;
            }
            // RIGHT
            else
            {
                Bitboard m = RAY_BB[sq, (int)Direction.Right];
                Bitboard t = occupancy & m;
                t |= t >> 1;
                t |= t >> 2;
                t |= t >> 4;
                t >>= 1;
                return m & ~t;
            }
        }

        public static Bitboard LanceAttacks(Color c, int sq, Bitboard occupancy)
        {
            return c == Color.Black
                ? SliderAttacks(Direction.Right, sq, occupancy)
                : SliderAttacks(Direction.Left, sq, occupancy);
        }

        public static Bitboard BishopAttacks(int sq, Bitboard occupancy)
        {
            return SliderAttacks(Direction.LeftUp   , sq, occupancy)
                 | SliderAttacks(Direction.RightUp  , sq, occupancy)
                 | SliderAttacks(Direction.RightDown, sq, occupancy)
                 | SliderAttacks(Direction.LeftDown , sq, occupancy);
        }

        public static Bitboard RookAttacks(int sq, Bitboard occupancy)
        {
            return SliderAttacks(Direction.Left , sq, occupancy)
                 | SliderAttacks(Direction.Up   , sq, occupancy)
                 | SliderAttacks(Direction.Right, sq, occupancy)
                 | SliderAttacks(Direction.Down , sq, occupancy);
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
