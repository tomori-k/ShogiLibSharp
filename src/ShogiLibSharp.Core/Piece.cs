using System.Collections;

namespace ShogiLibSharp.Core;

/// <summary>
/// 駒を表す列挙型。
/// </summary>
public enum Piece
{
    Empty = 0, Pawn, Lance, Knight,
    Silver, Gold, Bishop, Rook,
    King, ProPawn, ProLance, ProKnight,
    ProSilver, ProGold, ProBishop, ProRook,

    B_Empty = 0, B_Pawn, B_Lance, B_Knight,
    B_Silver, B_Gold, B_Bishop, B_Rook,
    B_King, B_ProPawn, B_ProLance, B_ProKnight,
    B_ProSilver, B_ProGold, B_ProBishop, B_ProRook,

    W_Empty, W_Pawn, W_Lance, W_Knight,
    W_Silver, W_Gold, W_Bishop, W_Rook,
    W_King, W_ProPawn, W_ProLance, W_ProKnight,
    W_ProSilver, W_ProGold, W_ProBishop, W_ProRook,

    None,

    KindMask = 0b00111, // 手番、成り情報を消すマスク（玉に対して使うとバグる）
    ColorlessMask = 0b01111, // 手番情報を消すマスク
    DemotionMask = 0b10111, // 成り情報を消すマスク
    ColorBit = 0b10000,
    PromotionBit = 0b01000, // 成りビット
}

/// <summary>
/// `Piece` の拡張メソッドを定義するクラス。
/// </summary>
public static class PieceExtensions
{
    public static Color Color(this Piece p)
    {
        return (Color)((uint)p >> 4);
    }

    /// <summary>
    /// p の成ビットを立てた Piece を作成
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Piece Promoted(this Piece p)
    {
        return p | Piece.PromotionBit;
    }

    /// <summary>
    /// p の成ビットを下ろした Piece を作成
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Piece Demoted(this Piece p)
    {
        return p & Piece.DemotionMask;
    }

    /// <summary>
    /// p を c の駒に変換した Piece を作成
    /// </summary>
    /// <param name="p"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Piece Colored(this Piece p, Color c)
    {
        return (p & Piece.ColorlessMask) | (Piece)((uint)c << 4);
    }

    /// <summary>
    /// p の Color ビット、成ビットを下ろした Piece を作成
    /// 玉（KING、B_KING、W_KING）に使うと EMPTY になるので注意
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Piece Kind(this Piece p)
    {
        return p & Piece.KindMask;
    }

    /// <summary>
    /// p の Color ビットを下ろした Piece を作成
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Piece Colorless(this Piece p)
    {
        return p & Piece.ColorlessMask;
    }

    /// <summary>
    /// p が成り駒か判定
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static bool IsPromoted(this Piece p)
    {
        return p.Colorless() > Piece.King;
    }
}

public static class Pieces
{
    public static readonly PawnToRookEnumerable PawnToRook = new();
    public static readonly RookToPawnEnumerable RookToPawn = new();

    public class PawnToRookEnumerable
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Piece>
        {
            int _piece = 0;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return ++this._piece <= (int)Piece.Rook;
            }

            public void Reset()
            {
                this._piece = 0;
            }

            public Piece Current => (Piece)this._piece;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }

    public class RookToPawnEnumerable
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator : IEnumerator<Piece>
        {
            int _piece = 8;

            public Enumerator()
            {
            }

            public bool MoveNext()
            {
                return --this._piece >= (int)Piece.Pawn;
            }

            public void Reset()
            {
                this._piece = 8;
            }

            public Piece Current => (Piece)this._piece;

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}