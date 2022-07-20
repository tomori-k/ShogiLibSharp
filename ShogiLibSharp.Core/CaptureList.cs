using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core
{
    /// <summary>
    /// 駒台
    /// </summary>
    public enum CaptureList : ulong
    {
        Zero = 0UL,
        ExceptPawnMask = 0xffffffffffff0000UL,
    }

    public static class CaptureListExtensions
    {
        /// <summary>
        /// 駒台をクリア
        /// </summary>
        public static void Clear(this ref CaptureList captureList)
        {
            captureList = CaptureList.Zero;
        }

        /// <summary>
        /// 駒台が空かどうか
        /// </summary>
        /// <returns></returns>
        public static bool None(this CaptureList captureList)
        {
            return captureList == CaptureList.Zero;
        }

        /// <summary>
        /// 駒台に駒があるかどうか
        /// </summary>
        /// <returns></returns>
        public static bool Any(this CaptureList captureList)
        {
            return !captureList.None();
        }

        /// <summary>
        /// 歩以外の駒を持っているか
        /// </summary>
        /// <returns></returns>
        public  static bool ExceptPawn(this CaptureList captureList)
        {
            return (captureList & CaptureList.ExceptPawnMask) != 0UL;
        }

        /// <summary>
        /// 所持している p の数を取得
        /// </summary>
        /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
        /// <returns></returns>
        public static int Count(this CaptureList captureList, Piece p)
        {
            return (int)((ulong)captureList >> ((int)p * 8) & 0xffUL);
        }

        /// <summary>
        /// 宣言勝ちにおける持ち駒の得点を計算
        /// </summary>
        /// <returns></returns>
        public static int Point(this CaptureList captureList)
        {
            ulong t = (ulong)captureList * 0x0101010101010101UL;
            int small = (int)(t >> 40 & 0xffUL);
            return ((int)(t >> 56) - small) * 5 + small;
        }

        /// <summary>
        /// 駒台に p の駒を cnt 個追加する
        /// </summary>
        /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
        /// <param name="cnt">負の数も指定可能。ただし、結果が負になると壊れる。</param>
        public static void Add(this ref CaptureList captureList, Piece p, int cnt)
        {
            captureList += (1UL << ((int)p * 8)) * (ulong)cnt;
        }

        /// <summary>
        /// 持ち駒の数について、どの種類の駒も comp の枚数以上となっているか
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool IsEqualOrSuperiorTo(this CaptureList captureList, CaptureList comp)
        {
            return ((captureList - comp) & 0x8080808080808080UL) == 0UL;
        }

        /// <summary>
        /// 持ち駒の総数を取得
        /// </summary>
        /// <returns></returns>
        public static int CountAll(this CaptureList captureList)
        {
            return (int)(((ulong)captureList * 0x0101010101010101UL) >> 56);
        }

        /// <summary>
        /// 歩以外の持ち駒の総数を取得
        /// </summary>
        /// <returns></returns>
        public static int CountExceptPawn(this CaptureList captureList)
        {
            return (int)(((ulong)(captureList & CaptureList.ExceptPawnMask) * 0x0101010101010101UL) >> 56);
        }

        /// <summary>
        /// 人が見やすいように変換
        /// </summary>
        /// <returns></returns>
        public static string Pretty(this CaptureList captureList)
        {
            if (captureList.Any())
            {
                var sb = new StringBuilder();
                foreach (var p in PieceExtensions.PawnToRook)
                {
                    if (captureList.Count(p) > 0)
                    {
                        sb.Append($"{p.Pretty()}{captureList.Count(p)}");
                    }
                }
                return sb.ToString();
            }
            else
            {
                return "なし";
            }
        }
    }
}
