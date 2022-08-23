using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core
{
    public static class Square
    {
        readonly static int[] rankTable = new[]
        {
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                0, 1, 2, 3, 4, 5, 6, 7, 8,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
                8, 7, 6, 5, 4, 3, 2, 1, 0,
        };

        static void ThrowIfOutOfBoard(int sq)
        {
            if (0 <= sq && sq < 81) return;
            throw new ArgumentOutOfRangeException($"マス番号が範囲外です。: {sq}");
        }

        /// <summary>
        /// 段
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <returns>
        /// 段番号（0 スタート）<br/>
        /// 例1: 先手目線でマス 0 の段は 0  <br/>
        /// 例2: 後手目線でマス 0 の段は 8  <br/>
        /// </returns>
        public static int RankOf(Color c, int sq)
        {
            ThrowIfOutOfBoard(sq);
            return RankOf_Unsafe(c, sq);
        }

        /// <summary>
        /// 段
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <returns>段番号（0 スタート）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RankOf_Unsafe(Color c, int sq)
        {
            var index = (-(int)c & 81) + sq;
            ref int tableRef = ref MemoryMarshal.GetArrayDataReference(rankTable);
            return Unsafe.Add(ref tableRef, (nint)index);
        }

        /// <summary>
        /// 段
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <returns>
        /// 段番号（0 スタート）
        /// </returns>
        public static int RankOf(int sq)
            => RankOf(Color.Black, sq);

        /// <summary>
        /// 筋（0 スタート）
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static int FileOf(int sq) => sq / 9;

        /// <summary>
        /// 段、筋（どちらも 0 スタート） → マス番号
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static int Index(int rank, int file) => rank + file * 9;
        
        /// <summary>
        /// 駒移動時に成れるか
        /// </summary>
        /// <param name="c"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool CanPromote(Color c, int from, int to)
            => RankOf(c, from) <= 2 || RankOf(c, to) <= 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanPromote_Unsafe(Color c, int from, int to)
            => RankOf_Unsafe(c, from) <= 2 || RankOf_Unsafe(c, to) <= 2;


        static readonly string[] PrettyRankTable = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        static readonly string[] PrettyFileTable = { "１", "２", "３", "４", "５", "６", "７", "８", "９" };

        /// <summary>
        /// 段を人が見やすい文字列に変換
        /// 例：rank=3 → 四
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static string PrettyRank(int rank)
            => PrettyRankTable[rank];

        /// <summary>
        /// 筋を人が見やすい文字列に変換
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string PrettyFile(int file)
            => PrettyFileTable[file];

        /// <summary>
        /// マス番号を人が読みやすい文字列に変換
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static string PrettySquare(int sq)
            => $"{PrettyFile(FileOf(sq))}{PrettyRank(RankOf(sq))}";
    }
}
