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

    /// <summary>
    /// USI 形式の文字列に変換する。
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string ToUsi(this Color c)
    {
        return c == Color.Black ? "b" : "w";
    }

    /// <summary>
    /// USI 形式の文字列に変換する。
    /// </summary>
    /// <param name="sq"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ToUsi(this Square sq)
    {
        if (!(Square.S11 <= sq && sq <= Square.S99))
        {
            throw new ArgumentException($"マス番号が範囲外です。");
        }

        var rank = sq.Rank();
        var file = sq.File();

        return $"{file + 1}{(char)('a' + rank)}";
    }

    /// <summary>
    /// USI 形式の文字列に変換
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ToUsi(this Piece p)
    {
        var t = p.Colorless() != Piece.King ? p.Demoted() : p;

        if (!PieceToChar.ContainsKey(t))
        {
            throw new ArgumentException($"有効な値ではありません。");
        }

        char c = PieceToChar[t];

        return p.IsPromoted() ? $"+{c}" : $"{c}";
    }

    /// <summary>
    /// USI 形式の指し手文字列に変換
    /// </summary>
    /// <returns></returns>
    public static string ToUsi(this Move m)
    {
        var to = m.To().ToUsi();

        if (m.IsDrop())
        {
            return $"{m.Dropped().ToUsi()}*{to}";
        }
        else
        {
            var from = m.From().ToUsi();
            var promote = m.IsPromote() ? "+" : "";

            return $"{from}{to}{promote}";
        }
    }
}
