namespace ShogiLibSharp.Core;

/// <summary>
/// 駒台
/// </summary>
public enum Hand : ulong
{
    Zero = 0UL,
    ExceptPawnMask = 0xffffffffffff0000UL,
}

public static class CaptureListExtensions
{
    /// <summary>
    /// 駒台が空かどうか調べる。
    /// </summary>
    /// <returns></returns>
    public static bool None(this Hand h)
    {
        return h == Hand.Zero;
    }

    /// <summary>
    /// 駒台に駒があるかどうか調べる。
    /// </summary>
    /// <returns></returns>
    public static bool Any(this Hand h)
    {
        return !h.None();
    }

    /// <summary>
    /// 歩以外の駒を持っているか調べる。
    /// </summary>
    /// <returns></returns>
    public static bool ExceptPawn(this Hand h)
    {
        return (h & Hand.ExceptPawnMask) != 0UL;
    }

    /// <summary>
    /// 所持している駒の数を取得する。
    /// </summary>
    /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
    /// <returns></returns>
    public static int Count(this Hand h, Piece p)
    {
        return (int)((ulong)h >> ((int)p * 8) & 0xffUL);
    }

    /// <summary>
    /// 宣言勝ちにおける持ち駒の得点を計算する。
    /// </summary>
    /// <returns></returns>
    public static int DeclarationPoint(this Hand h)
    {
        ulong t = (ulong)h * 0x0101010101010101UL;
        int small = (int)(t >> 40 & 0xffUL);
        return ((int)(t >> 56) - small) * 5 + small;
    }

    /// <summary>
    /// 駒台に p の駒を cnt 個追加する
    /// </summary>
    /// <param name="p">PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOKのどれか</param>
    /// <param name="cnt">負の数も指定可能。ただし、結果が負になると壊れる。</param>
    public static void Add(this ref Hand h, Piece p, int cnt)
    {
        h += (1UL << ((int)p * 8)) * (ulong)cnt;
    }

    /// <summary>
    /// 比較対象の駒台より、どの種類の駒も多いか等しいかどうかを判定する。
    /// </summary>
    /// <param name="comp"></param>
    /// <returns></returns>
    public static bool IsEqualOrSuperiorTo(this Hand h, Hand comp)
    {
        return ((h - comp) & 0x8080808080808080UL) == 0UL;
    }

    /// <summary>
    /// 持ち駒の総数を取得する。
    /// </summary>
    /// <returns></returns>
    public static int Count(this Hand h)
    {
        return (int)(((ulong)h * 0x0101010101010101UL) >> 56);
    }

    /// <summary>
    /// 歩以外の持ち駒の総数を取得する。
    /// </summary>
    /// <returns></returns>
    public static int CountExceptPawn(this Hand h)
    {
        return (h & Hand.ExceptPawnMask).Count();
    }
}
