using System.Collections;
using System.Runtime.CompilerServices;

namespace ShogiLibSharp.Core;

public enum Square
{
    S11, S12, S13, S14, S15, S16, S17, S18, S19,
    S21, S22, S23, S24, S25, S26, S27, S28, S29,
    S31, S32, S33, S34, S35, S36, S37, S38, S39,
    S41, S42, S43, S44, S45, S46, S47, S48, S49,
    S51, S52, S53, S54, S55, S56, S57, S58, S59,
    S61, S62, S63, S64, S65, S66, S67, S68, S69,
    S71, S72, S73, S74, S75, S76, S77, S78, S79,
    S81, S82, S83, S84, S85, S86, S87, S88, S89,
    S91, S92, S93, S94, S95, S96, S97, S98, S99,
}

/// <summary>
/// `Square` の拡張メソッドを定義するクラス。
/// </summary>
public static class SquareExtensions
{
    readonly static Rank[] RankTable = new[]
    {
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
        Core.Rank.R1, Core.Rank.R9, Core.Rank.R2, Core.Rank.R8, Core.Rank.R3, Core.Rank.R7, Core.Rank.R4, Core.Rank.R6, Core.Rank.R5, Core.Rank.R5, Core.Rank.R6, Core.Rank.R4, Core.Rank.R7, Core.Rank.R3, Core.Rank.R8, Core.Rank.R2, Core.Rank.R9, Core.Rank.R1,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int RankTableIndex(Color c, Square sq) => (int)sq * 2 + (int)c;

    /// <summary>
    /// 指定したプレイヤー側から見た、マス目の段を求める。<br/>
    /// 例1: 先手目線で `Square.S11` の段は `Rank.1` <br/>
    /// 例2: 後手目線で `Square.S11` の段は `Rank.9` <br/>
    /// </summary>
    /// <param name="c"></param>
    /// <param name="sq"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rank Rank(this Square sq, Color c) => RankTable[RankTableIndex(c, sq)];

    /// <summary>
    /// 段
    /// </summary>
    /// <param name="c"></param>
    /// <param name="sq"></param>
    /// <returns>
    /// 段番号（0 スタート）
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rank Rank(this Square sq) => sq.Rank(Color.Black);

    /// <summary>
    /// 筋
    /// </summary>
    /// <param name="sq"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static File File(this Square sq) => (File)((int)sq / 9);
}

/// <summary>
/// `Square` を扱う上で便利なものをまとめたクラス。
/// </summary>
public static class Squares
{
    /// <summary>
    /// 昇順ですべてのマスを列挙するイテレータのインスタンス。
    /// </summary>
    public static readonly Ascending All = new();

    /// <summary>
    /// 降順ですべてのマスを列挙するイテレータのインスタンス。
    /// </summary>
    public static readonly Descending Reversed = new();

    /// <summary>
    /// 段、筋からマス目の番号を求める。
    /// </summary>
    /// <param name="rank"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Square Index(Rank rank, File file) => (Square)((int)rank + (int)file * 9);

    /// <summary>
    /// 駒移動時に成れるか
    /// </summary>
    /// <param name="c"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanPromote(Color c, Square from, Square to)
        => from.Rank(c) <= Core.Rank.R3 || to.Rank(c) <= Core.Rank.R3;

    public class Ascending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Square>
        {
            int _sq = -1;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return ++this._sq < 81;
            }

            public void Reset()
            {
                this._sq = -1;
            }

            public Square Current => (Square)this._sq;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }

    public class Descending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Square>
        {
            int _sq = 81;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return --this._sq >= 0;
            }

            public void Reset()
            {
                this._sq = 81;
            }

            public Square Current => (Square)this._sq;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}