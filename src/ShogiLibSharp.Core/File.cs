using System.Collections;

namespace ShogiLibSharp.Core;

/// <summary>
/// 筋を表す列挙型。
/// </summary>
public enum File
{
    /// <summary>
    /// 1筋
    /// </summary>
    F1,

    /// <summary>
    /// 2筋
    /// </summary>
    F2,

    /// <summary>
    /// 3筋
    /// </summary>
    F3,

    /// <summary>
    /// 4筋
    /// </summary>
    F4,

    /// <summary>
    /// 5筋
    /// </summary>
    F5,

    /// <summary>
    /// 6筋
    /// </summary>
    F6,

    /// <summary>
    /// 7筋
    /// </summary>
    F7,

    /// <summary>
    /// 8筋
    /// </summary>
    F8,

    /// <summary>
    /// 9筋
    /// </summary>
    F9
}

public static class Files
{
    public static readonly Ascending All = new();
    public static readonly Descending Reversed = new();

    public class Ascending
    {
        public Enumerator GetEnumerator() => new();

        public struct Enumerator : IEnumerator<File>
        {
            int _file = -1;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return ++this._file <= (int)File.F9;
            }

            public void Reset()
            {
                this._file = -1;
            }

            public File Current => (File)this._file;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }

    public class Descending
    {
        public Enumerator GetEnumerator() => new();

        public struct Enumerator : IEnumerator<File>
        {
            int _file = 9;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return --this._file >= (int)File.F1;
            }

            public void Reset()
            {
                this._file = 9;
            }

            public File Current => (File)this._file;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}
