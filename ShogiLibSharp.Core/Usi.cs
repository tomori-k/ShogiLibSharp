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
        /// USIの定義に従い、c を Piece に変換
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Piece FromUsi(char c)
        {
            if (!CharToPiece.ContainsKey(c))
            {
                throw new FormatException($"文字を駒に変換できません：{c}");
            }
            return CharToPiece[c];
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
        {
            if (usiMove == "resign") return Move.Resign;
            else if (usiMove == "win") return Move.Win;

            if (usiMove.Length < 4)
            {
                throw new FormatException($"USI 形式ではありません：{usiMove}");
            }
            var to = ParseSquare(usiMove.Substring(2, 2));
            // 駒打ち
            if (usiMove[1] == '*')
            {
                var dropped = FromUsi(usiMove[0]);
                if (dropped.Color() != Color.Black)
                    throw new FormatException($"USI 形式ではありません：{usiMove}");
                return MoveExtensions.MakeDrop(dropped, to);
            }
            // 駒移動
            else
            {
                var from = ParseSquare(usiMove.Substring(0, 2));
                var promote = usiMove.Length >= 5 && usiMove[4] == '+';
                return MoveExtensions.MakeMove(from, to, promote);
            }
        }

        /// <summary>
        /// USI 形式のの座標指定文字列をマス番号に変換
        /// </summary>
        /// <param name="usiSq"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static int ParseSquare(string usiSq)
        {
            if (usiSq.Length < 2
                || !('1' <= usiSq[0] && usiSq[0] <= '9')
                || !('a' <= usiSq[1] && usiSq[1] <= 'i'))
            {
                throw new FormatException($"USI 形式ではありません：{usiSq}");
            }
            var file = usiSq[0] - '1';
            var rank = usiSq[1] - 'a';
            return Core.Square.Index(rank, file);
        }
    }
}
