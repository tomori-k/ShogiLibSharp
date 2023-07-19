namespace ShogiLibSharp.Core;

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
    public static Square To(this Move m)
    {
        return (Square)(m & Move.ToMask);
    }

    /// <summary>
    /// 移動元
    /// </summary>
    /// <returns></returns>
    public static Square From(this Move m)
    {
        return (Square)((int)m >> 7 & (int)Move.ToMask);
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
    /// from から to に動かす指し手を生成
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="promote">成る指し手かどうか</param>
    public static Move MakeMove(Square from, Square to, bool promote = false)
    {
        return (Move)(to + ((int)from << 7) + (Convert.ToInt32(promote) << 14));
    }

    /// <summary>
    /// p の駒を to に打つ指し手を生成
    /// </summary>
    /// <param name="p"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static Move MakeDrop(Piece p, Square to)
    {
        return (Move)(to + ((int)p << 7) + (1 << 15));
    }
}
