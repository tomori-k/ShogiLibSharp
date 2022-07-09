using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp
{
    /// <summary>
    /// 指し手
    /// </summary>
    public struct Move : IEquatable<Move>
    {
        private readonly ushort m;

        public static readonly Move MoveNone = default;
        public static readonly Move MoveWin = MakeMove(2, 2, false);
        public static readonly Move MoveResign = MakeMove(3, 3, false);

        /// <summary>
        /// 移動先
        /// </summary>
        /// <returns></returns>
        public int To()
        {
            return m & 0x7f;
        }

        /// <summary>
        /// 移動元
        /// </summary>
        /// <returns></returns>
        public int From()
        {
            return m >> 7 & 0x7f;
        }

        /// <summary>
        /// 打つ駒の種類
        /// </summary>
        /// <returns>PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOK のどれか</returns>
        public Piece Dropped()
        {
            return (Piece)From();
        }

        /// <summary>
        /// 成る指し手か
        /// </summary>
        /// <returns></returns>
        public bool IsPromote()
        {
            return (m & (1 << 14)) != 0;
        }

        /// <summary>
        /// 駒打ちか
        /// </summary>
        /// <returns></returns>
        public bool IsDrop()
        {
            return (m & (1 << 15)) != 0;
        }

        /// <summary>
        /// USI 形式の指し手文字列に変換
        /// </summary>
        /// <returns></returns>
        public string Usi()
        {
            var to = ShogiLibSharp.Usi.Square(To());
            if (IsDrop())
            {
                return $"{Dropped().Usi()}*{to}";
            }
            else
            {
                var from = ShogiLibSharp.Usi.Square(From());
                var promote = (IsPromote() ? "+" : "");
                return $"{from}{to}{promote}";
            }
        }


        public Move(ushort m)
        {
            this.m = m;
        }

        public bool Equals(Move other)
        {
            return this.m == other.m;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Move move && Equals(move);
        }

        public override int GetHashCode()
        {
            return m;
        }

        public override string ToString()
        {
            return Usi();
        }

        public static bool operator ==(Move lhs, Move rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Move lhs, Move rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// from から to に動かす指し手を生成
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="promote">成る指し手かどうか</param>
        public static Move MakeMove(int from, int to, bool promote = false)
        {
            return new Move((ushort)(to + (from << 7) + (Convert.ToInt32(promote) << 14)));
        }

        /// <summary>
        /// p の駒を to に打つ指し手を生成
        /// </summary>
        /// <param name="p"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Move MakeDrop(Piece p, int to)
        {
            return new Move((ushort)(to + ((int)p << 7) + (1 << 15)));
        }
    }
}
