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
    public enum Move : ushort
    {
        None = 0,
        Win = 2 + (2 << 7),
        Resign = 3 + (3 << 7),
        ToMask = 0x7f,
        PromoteBit = 0x4000,
        DropBit = 0x8000,
    }

    public static class MoveExtensions
    {
        /// <summary>
        /// 移動先
        /// </summary>
        /// <returns></returns>
        public static int To(this Move m)
        {
            return (int)(m & Move.ToMask);
        }

        /// <summary>
        /// 移動元
        /// </summary>
        /// <returns></returns>
        public static int From(this Move m)
        {
            return (int)m >> 7 & (int)Move.ToMask;
        }

        /// <summary>
        /// 打つ駒の種類
        /// </summary>
        /// <returns>Pawn, Lance, Knight, Silver, Gold, Bishop, Rook のどれか</returns>
        public static Piece Dropped(this Move m)
        {
            return (Piece)m.From();
        }

        /// <summary>
        /// 成る指し手か
        /// </summary>
        /// <returns></returns>
        public static bool IsPromote(this Move m)
        {
            return (m & Move.PromoteBit) != 0;
        }

        /// <summary>
        /// 駒打ちか
        /// </summary>
        /// <returns></returns>
        public static bool IsDrop(this Move m)
        {
            return (m & Move.DropBit) != 0;
        }

        /// <summary>
        /// USI 形式の指し手文字列に変換
        /// </summary>
        /// <returns></returns>
        public static string Usi(this Move m)
        {
            var to = ShogiLibSharp.Usi.Square(m.To());
            if (m.IsDrop())
            {
                return $"{m.Dropped().Usi()}*{to}";
            }
            else
            {
                var from = ShogiLibSharp.Usi.Square(m.From());
                var promote = (m.IsPromote() ? "+" : "");
                return $"{from}{to}{promote}";
            }
        }

        /// <summary>
        /// from から to に動かす指し手を生成
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="promote">成る指し手かどうか</param>
        public static Move MakeMove(int from, int to, bool promote = false)
        {
            return (Move)(to + (from << 7) + (Convert.ToInt32(promote) << 14));
        }

        /// <summary>
        /// p の駒を to に打つ指し手を生成
        /// </summary>
        /// <param name="p"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Move MakeDrop(Piece p, int to)
        {
            return (Move)(to + ((int)p << 7) + (1 << 15));
        }
    }
}
