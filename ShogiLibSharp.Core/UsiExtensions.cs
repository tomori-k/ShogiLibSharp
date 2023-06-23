namespace ShogiLibSharp.Core;

public static class UsiExtensions
{
    static readonly Dictionary<Piece, char> PieceToChar = new()
    {
        { Piece.B_Pawn  , 'P' },
        { Piece.B_Lance , 'L' },
        { Piece.B_Knight, 'N' },
        { Piece.B_Silver, 'S' },
        { Piece.B_Gold  , 'G' },
        { Piece.B_Bishop, 'B' },
        { Piece.B_Rook  , 'R' },
        { Piece.B_King  , 'K' },
        { Piece.W_Pawn  , 'p' },
        { Piece.W_Lance , 'l' },
        { Piece.W_Knight, 'n' },
        { Piece.W_Silver, 's' },
        { Piece.W_Gold  , 'g' },
        { Piece.W_Bishop, 'b' },
        { Piece.W_Rook  , 'r' },
        { Piece.W_King  , 'k' },
    };

    public static string ToUsi(this Color c)
    {
        return c == Color.Black ? "b" : "w";
    }

    /// <summary>
    /// USI 形式の文字列に変換
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static string ToUsi(this Piece p)
    {
        var t = p.Colorless() != Piece.King ? p.Demoted() : p;
        if (!PieceToChar.ContainsKey(t))
        {
            throw new FormatException($"Piece: {p} が有効な値ではありません");
        }
        char c = PieceToChar[t];
        return p.Colorless() != Piece.King && p.IsPromoted()
            ? $"+{c}" : $"{c}";
    }
}
