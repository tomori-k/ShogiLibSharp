using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core
{
    public static class Usi
    {
        private static readonly Dictionary<char, Piece> CharToPiece
            = new Dictionary<char, Piece> {
                {'P', Piece.B_Pawn  },
                {'L', Piece.B_Lance },
                {'N', Piece.B_Knight},
                {'S', Piece.B_Silver},
                {'G', Piece.B_Gold  },
                {'B', Piece.B_Bishop},
                {'R', Piece.B_Rook  },
                {'K', Piece.B_King  },
                {'p', Piece.W_Pawn  },
                {'l', Piece.W_Lance },
                {'n', Piece.W_Knight},
                {'s', Piece.W_Silver},
                {'g', Piece.W_Gold  },
                {'b', Piece.W_Bishop},
                {'r', Piece.W_Rook  },
                {'k', Piece.W_King  },
            };

        /// <summary>
        /// USI形式の駒文字 c を Piece に変換
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Piece FromUsi(char c)
            => TryParsePiece(c, out var p) ? p : throw new FormatException($"文字を駒に変換できません：{c}");

        /// <summary>
        /// USI形式の駒文字 c を Piece に変換
        /// </summary>
        /// <param name="c"></param>
        public static bool TryParsePiece(char c, out Piece piece)
        {
            if (CharToPiece.ContainsKey(c))
            {
                piece = CharToPiece[c];
                return true;
            }
            else
            {
                piece = Piece.None;
                return false;
            }
        }

        /// <summary>
        /// マス sq を USI 形式の文字列に変換
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string Square(int sq)
        {
            if (!(0 <= sq && sq < 81))
            {
                throw new ArgumentException($"マス番号が範囲外です：{sq}");
            }
            var rank = Core.Square.RankOf(sq);
            var file = Core.Square.FileOf(sq);
            return $"{file + 1}{(char)('a' + rank)}";
        }

        /// <summary>
        /// USI 形式の指し手文字列を Move に変換
        /// </summary>
        /// <param name="usiMove"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Move ParseMove(string usiMove)
            => TryParseMove(usiMove, out var m) ? m : throw new FormatException($"USI 形式ではありません：{usiMove}");

        /// <summary>
        /// USI 形式の指し手文字列を Move に変換
        /// </summary>
        /// <param name="usiMove"></param>
        public static bool TryParseMove(string usiMove, out Move result)
        {
            if (usiMove == "resign" || usiMove == "win")
            {
                result = usiMove.StartsWith('r') ? Move.Resign : Move.Win;
                return true;
            }

            if (usiMove.Length < 4
                || !TryParseSquare(usiMove[2..4], out var to))
            {
                result = Move.None;
                return false;
            }

            // 駒打ち
            if (usiMove[1] == '*')
            {
                if (!TryParsePiece(usiMove[0], out var dropped)
                    || dropped.Color() != Color.Black)
                {
                    result = Move.None;
                    return false;
                }
                result = MoveExtensions.MakeDrop(dropped, to);
            }
            // 駒移動
            else
            {
                if (!TryParseSquare(usiMove[0..2], out var from))
                {
                    result = Move.None;
                    return false;
                }
                var promote = usiMove.Length >= 5 && usiMove[4] == '+';
                result = MoveExtensions.MakeMove(from, to, promote);
            }

            return true;
        }

        /// <summary>
        /// USI 形式のの座標指定文字列をマス番号に変換
        /// </summary>
        /// <param name="usiSq"></param>
        /// <exception cref="FormatException"></exception>
        private static int ParseSquare(string usiSq)
            => TryParseSquare(usiSq, out var sq) ? sq : throw new FormatException($"USI 形式ではありません：{usiSq}");

        /// <summary>
        /// USI 形式のの座標指定文字列をマス番号に変換
        /// </summary>
        /// <param name="usiSq"></param>
        private static bool TryParseSquare(string usiSq, out int sq)
        {
            if (usiSq.Length < 2
                || !('1' <= usiSq[0] && usiSq[0] <= '9')
                || !('a' <= usiSq[1] && usiSq[1] <= 'i'))
            {
                sq = 0;
                return false;
            }
            var file = usiSq[0] - '1';
            var rank = usiSq[1] - 'a';
            sq = Core.Square.Index(rank, file);
            return true;
        }
    }
}
