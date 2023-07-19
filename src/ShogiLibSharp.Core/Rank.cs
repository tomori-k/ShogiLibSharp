using System.Collections;

namespace ShogiLibSharp.Core;

/// <summary>
/// 段を表す列挙型。
/// </summary>
public enum Rank
{
    /// <summary>
    /// 一段目
    /// </summary>
    R1,

    /// <summary>
    /// 二段目
    /// </summary>
    R2,

    /// <summary>
    /// 三段目
    /// </summary>
    R3,

    /// <summary>
    /// 四段目
    /// </summary>
    R4,

    /// <summary>
    /// 五段目
    /// </summary>
    R5,

    /// <summary>
    /// 六段目
    /// </summary>
    R6,

    /// <summary>
    /// 七段目
    /// </summary>
    R7,

    /// <summary>
    /// 八段目
    /// </summary>
    R8,

    /// <summary>
    /// 九段目
    /// </summary>
    R9
}

public static class Ranks
{
    public static readonly Ascending All = new();
    public static readonly Descending Reversed = new();

    public class Ascending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Rank>
        {
            int _rank = -1;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return ++this._rank <= (int)Rank.R9;
            }

            public void Reset()
            {
                this._rank = -1;
            }

            public Rank Current => (Rank)this._rank;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }

    public class Descending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Rank>
        {
            int _rank = 9;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return --this._rank >= (int)Rank.R1;
            }

            public void Reset()
            {
                this._rank = 9;
            }

            public Rank Current => (Rank)this._rank;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}