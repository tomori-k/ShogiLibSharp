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
        /// USI形式の駒文字を `Piece` に変換する。
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Piece ParsePiece(char c)
            => TryParsePiece(c, out var p) ? p : throw new FormatException($"USI 形式の駒文字ではありません。");

        /// <summary>
        /// USI形式の駒文字を `Piece` に変換する。
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
        /// USI 形式の指し手文字列を `Move` に変換する。
        /// </summary>
        /// <param name="usiMove"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Move ParseMove(string usiMove)
            => TryParseMove(usiMove, out var m) ? m : throw new FormatException($"指し手文字列の形式が間違っています。");

        /// <summary>
        /// USI 形式の指し手文字列を `Move` に変換する。
        /// </summary>
        /// <param name="usiMove"></param>
        public static bool TryParseMove(string usiMove, out Move m)
        {
            if (usiMove == "resign" || usiMove == "win")
            {
                m = usiMove.StartsWith('r') ? Move.Resign : Move.Win;
                return true;
            }

            if (usiMove.Length < 4
                || !TryParseSquare(usiMove[2..4], out var to))
            {
                m = Move.None;
                return false;
            }

            // 駒打ち
            if (usiMove[1] == '*')
            {
                if (!TryParsePiece(usiMove[0], out var dropped)
                    || dropped.Color() != Color.Black)
                {
                    m = Move.None;
                    return false;
                }

                m = MoveExtensions.MakeDrop(dropped, to);
            }
            // 駒移動
            else
            {
                if (!TryParseSquare(usiMove[0..2], out var from))
                {
                    m = Move.None;
                    return false;
                }

                var promote = usiMove.Length >= 5 && usiMove[4] == '+';

                m = MoveExtensions.MakeMove(from, to, promote);
            }

            return true;
        }

        /// <summary>
        /// USI 形式のの座標文字列をマス番号に変換する。
        /// </summary>
        /// <param name="usiSq"></param>
        /// <exception cref="FormatException"></exception>
        private static Square ParseSquare(string usiSq)
            => TryParseSquare(usiSq, out var sq) ? sq : throw new FormatException($"USI 形式ではありません。");

        /// <summary>
        /// USI 形式のの座標指定文字列をマス番号に変換
        /// </summary>
        /// <param name="usiSq"></param>
        private static bool TryParseSquare(string usiSq, out Square sq)
        {
            if (usiSq.Length < 2
                || !('1' <= usiSq[0] && usiSq[0] <= '9')
                || !('a' <= usiSq[1] && usiSq[1] <= 'i'))
            {
                sq = Square.S11;
                return false;
            }

            var file = usiSq[0] - '1';
            var rank = usiSq[1] - 'a';
            sq = Squares.Index((Rank)rank, (File)file);

            return true;
        }
    }
}
