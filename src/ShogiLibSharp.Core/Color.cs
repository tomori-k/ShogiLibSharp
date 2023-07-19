using System.Collections;

namespace ShogiLibSharp.Core;

/// <summary>
/// 先後を表す列挙型。
/// </summary>
public enum Color
{
    Black, White
}

/// <summary>
/// `Color` の拡張メソッドを定義するクラス。
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// 先後を反転させます。
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Color Inv(this Color c)
    {
        return (Color)((int)c ^ 1);
    }
}

public static class Colors
{
    public static readonly Ascending All = new();
    public static readonly Descending Reversed = new();

    public class Ascending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Color>
        {
            int _c = -1;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return ++this._c <= (int)Color.White;
            }

            public void Reset()
            {
                this._c = -1;
            }

            public Color Current => (Color)this._c;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }

    public class Descending
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Color>
        {
            int _c = 2;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return --this._c >= 0;
            }

            public void Reset()
            {
                this._c = 2;
            }

            public Color Current => (Color)this._c;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}
