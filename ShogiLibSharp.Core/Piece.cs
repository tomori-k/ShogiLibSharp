using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core
{
    /// <summary>
    /// 将棋の駒
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
    /// Piece の拡張メソッドを定義するクラス
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
            return (p & Piece.PromotionBit) != Piece.Empty
               && p.Colorless() != Piece.King;
        }

        /// <summary>
        /// PAWN, LANCE, KNIGHT, SILVER, GOLD, BISHOP, ROOK をこの順に格納した配列
        /// </summary>
        public static readonly Piece[] PawnToRook
            = new[] { Piece.Pawn, Piece.Lance, Piece.Knight, Piece.Silver, Piece.Gold, Piece.Bishop, Piece.Rook };

        private static readonly string[] PrettyPiece
            = { "・", "歩", "香", "桂", "銀", "金", "角", "飛", "玉", "と", "杏", "圭", "全", "??", "馬", "竜",
                "??", "v歩", "v香", "v桂", "v銀", "v金", "v角", "v飛", "v玉", "vと", "v杏", "v圭", "v全", "??", "v馬", "v竜" };

        private static readonly Dictionary<Piece, char> PieceToChar
            = new Dictionary<Piece, char> {
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

        static readonly Dictionary<Piece, string> PieceToCsaNoColor = new Dictionary<Piece, string>
        {
            { Piece.Pawn     , "FU" },
            { Piece.Lance    , "KY" },
            { Piece.Knight   , "KE" },
            { Piece.Silver   , "GI" },
            { Piece.Gold     , "KI" },
            { Piece.Bishop   , "KA" },
            { Piece.Rook     , "HI" },
            { Piece.King     , "OU" },
            { Piece.ProPawn  , "TO" },
            { Piece.ProLance , "NY" },
            { Piece.ProKnight, "NK" },
            { Piece.ProSilver, "NG" },
            { Piece.ProBishop, "UM" },
            { Piece.ProRook  , "RY" },
        };

        static readonly Dictionary<Piece, string> PieceToCsa = new Dictionary<Piece, string>
        {
            { Piece.B_Pawn     , "+FU" },
            { Piece.B_Lance    , "+KY" },
            { Piece.B_Knight   , "+KE" },
            { Piece.B_Silver   , "+GI" },
            { Piece.B_Gold     , "+KI" },
            { Piece.B_Bishop   , "+KA" },
            { Piece.B_Rook     , "+HI" },
            { Piece.B_King     , "+OU" },
            { Piece.B_ProPawn  , "+TO" },
            { Piece.B_ProLance , "+NY" },
            { Piece.B_ProKnight, "+NK" },
            { Piece.B_ProSilver, "+NG" },
            { Piece.B_ProBishop, "+UM" },
            { Piece.B_ProRook  , "+RY" },
            { Piece.W_Pawn     , "-FU" },
            { Piece.W_Lance    , "-KY" },
            { Piece.W_Knight   , "-KE" },
            { Piece.W_Silver   , "-GI" },
            { Piece.W_Gold     , "-KI" },
            { Piece.W_Bishop   , "-KA" },
            { Piece.W_Rook     , "-HI" },
            { Piece.W_King     , "-OU" },
            { Piece.W_ProPawn  , "-TO" },
            { Piece.W_ProLance , "-NY" },
            { Piece.W_ProKnight, "-NK" },
            { Piece.W_ProSilver, "-NG" },
            { Piece.W_ProBishop, "-UM" },
            { Piece.W_ProRook  , "-RY" },
        };

        /// <summary>
        /// USI 形式の文字列に変換
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static string Usi(this Piece p)
        {
            var t = p.Colorless() != Piece.King
                ? p.Demoted() : p;
            if (!PieceToChar.ContainsKey(t))
            {
                throw new FormatException($"Piece: {p} が有効な値ではありません");
            }
            char c = PieceToChar[t];
            return p.Colorless() != Piece.King && p.IsPromoted()
                ? $"+{c}" : $"{c}";
        }

        /// <summary>
        /// CSA 形式の文字列に変換
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static string Csa(this Piece p)
        {
            if (!PieceToCsa.ContainsKey(p))
            {
                throw new FormatException($"Piece: {p} が有効な値ではありません");
            }
            return PieceToCsa[p];
        }

        /// <summary>
        /// CSA 形式の文字列（符号なし）に変換
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static string CsaNoColor(this Piece p)
        {
            p = p.Colorless();
            if (!PieceToCsaNoColor.ContainsKey(p))
            {
                throw new FormatException($"Piece: {p} が有効な値ではありません");
            }
            return PieceToCsaNoColor[p];
        }

        /// <summary>
        /// 人が見やすい文字列に変換
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string Pretty(this Piece p)
        {
            return PrettyPiece[(int)p];
        }
    }
}
