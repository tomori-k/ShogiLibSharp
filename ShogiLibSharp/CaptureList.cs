using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp
{
    /// <summary>
    /// 駒台
    /// </summary>
    public class CaptureList
    {
        private ulong caps;

        public CaptureList()
        {
            this.caps = 0;
        }

        public CaptureList(ulong caps)
        {
            this.caps = caps;
        }

        /// <summary>
        /// 駒台をクリア
        /// </summary>
        public void Clear()
        {
            caps = 0UL;
        }

        /// <summary>
        /// 駒台が空かどうか
        /// </summary>
        /// <returns></returns>
        public bool None()
        {
            return caps == 0UL;
        }

        /// <summary>
        /// 駒台に駒があるかどうか
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return !None();
        }

        /// <summary>
        /// 歩以外の駒を持っているか
        /// </summary>
        /// <returns></returns>
        public bool ExceptPawn()
        {
            return (caps & 0xffffffffffffff00UL) != 0UL;
        }

        /// <summary>
        /// 所持している p の数を取得
        /// </summary>
        /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
        /// <returns></returns>
        public int Count(Piece p)
        {
            return (int)(caps >> (((int)p - 1) * 8) & 0xffUL);
        }

        /// <summary>
        /// 宣言勝ちにおける持ち駒の得点を計算
        /// </summary>
        /// <returns></returns>
        public int Point()
        {
            ulong t = caps * 0x0101010101010101UL;
            int small = (int)(t >> 32 & 0xffUL);
            return ((int)(t >> 56) - small) * 5 + small;
        }

        /// <summary>
        /// 駒台に p の駒を cnt 個追加する
        /// </summary>
        /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
        /// <param name="cnt">負の数も指定可能。ただし、結果が負になると壊れる。</param>
        public void Add(Piece p, int cnt)
        {
            caps += (1UL << (((int)p - 1) * 8)) * (ulong)cnt;
        }

        /// <summary>
        /// 持ち駒の数について、どの種類の駒も comp の枚数以上となっているか
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public bool IsEqualOrSuperiorTo(CaptureList comp)
        {
            return ((caps - comp.caps) & 0x8080808080808080UL) == 0UL;
        }

        /// <summary>
        /// 持ち駒の総数を取得
        /// </summary>
        /// <returns></returns>
        public int CountAll()
        {
            return (int)((caps * 0x0101010101010101UL) >> 56);
        }

        /// <summary>
        /// 歩以外の持ち駒の総数を取得
        /// </summary>
        /// <returns></returns>
        public int CountExceptPawn()
        {
            return (int)(((caps & ~0xFFUL) * 0x0101010101010101UL) >> 56);
        }

        public CaptureList Clone()
        {
            return new CaptureList(caps);
        }

        /// <summary>
        /// 人が見やすいように変換
        /// </summary>
        /// <returns></returns>
        public string Pretty()
        {
            if (Any())
            {
                var sb = new StringBuilder();
                foreach (var p in PieceExtensions.PawnToRook)
                {
                    if (Count(p) > 0)
                    {
                        sb.Append($"{p.Pretty()}{Count(p)}");
                    }
                }
                return sb.ToString();
            }
            else
            {
                return "なし";
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is CaptureList list && this.caps == list.caps;
        }

        // todo: 32bit へのパック、あとで実装
        public override int GetHashCode()
        {
            return HashCode.Combine(caps);
        }
    }
}
