namespace ShogiLibSharp.Core
{
    public static class Csa
    {
        /// <summary>
        /// CSA 形式の棋譜の開始局面をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="FormatException"></exception>
        public static Position ParseStartPosition(Queue<string> lines)
        {
            var board = new Board();

            if (lines.TryPeek(out var firstLine) && firstLine.StartsWith("PI"))
            {
                // todo
                throw new NotImplementedException();
            }
            else
            {
                // 一括表現
                for (int rank = 0; rank < 9; ++rank)
                {
                    if (!lines.TryDequeue(out var line) || line.Length < 3 * 9 + 2)
                    {
                        throw new FormatException($"盤面情報が欠けています：{line}");
                    }

                    for (int file = 0; file < 9; ++file)
                    {
                        var sq = Core.Square.Index(rank, file);
                        var pieceStr = line.Substring(2 + (8 - file) * 3, 3);

                        if (pieceStr != " * ")
                        {
                            board.Squares[sq] = ParsePiece(pieceStr);
                        }
                    }
                }
                // 駒別単独表現
                while (lines.Count > 0)
                {
                    var next = lines.Peek();

                    if (!next.StartsWith("P"))
                    {
                        break;
                    }

                    lines.Dequeue();

                    var pc = next.StartsWith("P+")
                        ? Color.Black : Color.White;

                    for (int i = 2; i + 4 < next.Length; i += 4)
                    {
                        var squareStr = next.Substring(i, 2);
                        var pieceStr = next.Substring(i + 2, 2);
                        if (!TryParsePiece(pieceStr, out var piece))
                        {
                            throw new FormatException($"駒の形式が正しくありません：{next}");
                        }
                        // todo: AL対応
                        if (squareStr == "00")
                        {
                            board.CaptureListOf(pc).Add(piece, 1);
                        }
                        else
                        {
                            if (!TryParseSquare(squareStr, out var sq))
                            {
                                throw new FormatException($"駒の位置が正しくありません：{next}");
                            }
                            board.Squares[sq] = piece.Colored(pc);
                        }
                    }
                }
            }

            // 手番
            if (!lines.TryDequeue(out var colorStr))
            {
                throw new FormatException("開始局面での手番の情報がありません。");
            }
            board.Player = colorStr == "+" ? Color.Black : Color.White;

            return new Position(board);
        }

        /// <summary>
        /// CSA 形式の棋譜の指し手・消費時間をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startpos"></param>
        /// <returns></returns>
        public static List<(Move, TimeSpan?)> ParseMoves(Queue<string> lines, Position position)
        {
            var pos = position.Clone();
            var moves = new List<(Move, TimeSpan?)>();

            while (lines.Count > 0)
            {
                var moveStr = lines.Dequeue();
                if (!(moveStr.StartsWith("+") || moveStr.StartsWith("-")))
                {
                    break;
                }

                var move = ParseMove(moveStr, pos);
                pos.DoMove(move);

                if (!lines.TryPeek(out var timeStr)) continue;

                // 消費時間はオプションなので、ないこともある
                if (timeStr.StartsWith("T"))
                {
                    lines.Dequeue();
                    moves.Add((move, ParseTime(timeStr)));
                }
                else
                    moves.Add((move, null));
            }

            return moves;
        }

        /// <summary>
        /// CSA 形式の棋譜の指し手・消費時間をパース（消費時間がカンマで同じ行にあるパターン）
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startpos"></param>
        /// <returns></returns>
        public static List<(Move, TimeSpan)> ParseMovesWithTime(Queue<string> lines, Position position)
        {
            var pos = position.Clone();
            var moves = new List<(Move, TimeSpan)>();

            while (lines.Count > 0)
            {
                var moveStr = lines.Dequeue();
                if (!(moveStr.StartsWith("+") || moveStr.StartsWith("-")))
                {
                    break;
                }

                var (move, time) = ParseMoveWithTime(moveStr, pos);
                moves.Add((move, time));
                pos.DoMove(move);
            }

            return moves;
        }

        public static (Move, TimeSpan) ParseMoveWithTime(string s, Position pos)
        {
            var sp = s.Split(',')
                .Select(x => x.Trim())
                .ToArray();
            if (sp.Length < 2
                || !TryParseMove(sp[0], pos, out var move)
                || !TryParseTime(sp[1], out var time))
            {
                throw new FormatException($"指し手の形式が正しくありません。：{s}");
            }
            return (move, time);
        }

        public static TimeSpan ParseTime(string timeStr)
        {
            if (!TryParseTime(timeStr, out var time))
            {
                throw new FormatException("消費時間の形式が正しくありません。：");
            }
            return time;
        }

        public static bool TryParseTime(string timeStr, out TimeSpan time)
        {
            if (timeStr.StartsWith("T")
                && int.TryParse(timeStr[1..], out var sec))
            {
                time = TimeSpan.FromSeconds(sec);
                return true;
            }
            else
            {
                time = TimeSpan.Zero;
                return false;
            }
        }

        /// <summary>
        /// CSA 形式の棋譜で用いられる指し手文字列をパース
        /// </summary>
        /// <param name="moveStr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Move ParseMove(string moveStr, Position pos)
        {
            if (!TryParseMove(moveStr, pos, out var move))
            {
                throw new FormatException($"CSA 形式の指し手文字列ではありません：{moveStr}");
            }
            return move;
        }

        /// <summary>
        /// CSA 形式の棋譜で用いられる指し手文字列をパース
        /// </summary>
        /// <param name="moveStr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static bool TryParseMove(string moveStr, Position pos, out Move move)
        {
            if (moveStr.Length < 7
                || !TryParseSquare(moveStr[3..5], out var to))
            {
                move = Move.None;
                return false;
            }
            // 駒打ち
            if (moveStr[1..3] == "00")
            {
                if (TryParsePiece(moveStr[5..7], out var piece))
                {
                    move = MoveExtensions.MakeDrop(piece, to);
                    return true;
                }
                else
                {
                    move = Move.None;
                    return false;
                }
            }
            else
            {
                if (!TryParseSquare(moveStr[1..3], out var from))
                {
                    move = Move.None;
                    return false;
                }

                var pieceAfterStr = moveStr[0] + moveStr[5..7];
                if (!TryParsePiece(pieceAfterStr, out var pieceAfter))
                {
                    move = Move.None;
                    return false;
                }

                var promote = !pos.PieceAt(from).IsPromoted()
                    && pieceAfter.IsPromoted();
                move = MoveExtensions.MakeMove(from, to, promote);

                return true;
            }
        }

        /// <summary>
        /// CSA 形式の棋譜で用いられる座標文字列をパース
        /// </summary>
        /// <param name="squareStr"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static int ParseSquare(string squareStr)
        {
            if (TryParseSquare(squareStr, out var sq))
            {
                throw new FormatException($"CSA 形式の座標文字列ではありません：{squareStr}");
            }
            return sq;
        }

        /// <summary>
        /// CSA 形式の棋譜で用いられる座標文字列をパース
        /// </summary>
        /// <param name="squareStr"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static bool TryParseSquare(string squareStr, out int v)
        {
            if (squareStr.Length >= 2
                && ('1' <= squareStr[0] && squareStr[0] <= '9')
                && ('1' <= squareStr[1] && squareStr[1] <= '9'))
            {
                v = Core.Square.Index(squareStr[1] - '1', squareStr[0] - '1');
                return true;
            }
            else
            {
                v = 0;
                return false;
            }
        }

        private static readonly Dictionary<string, Piece> CsaToPiece = new()
        {
            { "FU", Piece.Pawn},
            { "KY", Piece.Lance },
            { "KE", Piece.Knight },
            { "GI", Piece.Silver },
            { "KI", Piece.Gold },
            { "KA", Piece.Bishop },
            { "HI", Piece.Rook },
            { "OU", Piece.King },
            { "TO", Piece.ProPawn},
            { "NY", Piece.ProLance },
            { "NK", Piece.ProKnight },
            { "NG", Piece.ProSilver },
            { "UM", Piece.ProBishop },
            { "RY", Piece.ProRook },
            { "+FU", Piece.B_Pawn},
            { "+KY", Piece.B_Lance },
            { "+KE", Piece.B_Knight },
            { "+GI", Piece.B_Silver },
            { "+KI", Piece.B_Gold },
            { "+KA", Piece.B_Bishop },
            { "+HI", Piece.B_Rook },
            { "+OU", Piece.B_King },
            { "+TO", Piece.B_ProPawn},
            { "+NY", Piece.B_ProLance },
            { "+NK", Piece.B_ProKnight },
            { "+NG", Piece.B_ProSilver },
            { "+UM", Piece.B_ProBishop },
            { "+RY", Piece.B_ProRook },
            { "-FU", Piece.W_Pawn},
            { "-KY", Piece.W_Lance },
            { "-KE", Piece.W_Knight },
            { "-GI", Piece.W_Silver },
            { "-KI", Piece.W_Gold },
            { "-KA", Piece.W_Bishop },
            { "-HI", Piece.W_Rook },
            { "-OU", Piece.W_King },
            { "-TO", Piece.W_ProPawn},
            { "-NY", Piece.W_ProLance },
            { "-NK", Piece.W_ProKnight },
            { "-NG", Piece.W_ProSilver },
            { "-UM", Piece.W_ProBishop },
            { "-RY", Piece.W_ProRook },
        };

        /// <summary>
        /// CSA 形式の棋譜で用いられる駒文字列をパース
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Piece ParsePiece(string ps)
        {
            if (!TryParsePiece(ps, out var piece))
            {
                throw new FormatException($"CSA 形式の駒文字列ではありません：{ps}");
            }
            return piece;
        }

        /// <summary>
        /// CSA 形式の棋譜で用いられる駒文字列をパース
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static bool TryParsePiece(string ps, out Piece v)
        {
            if (CsaToPiece.ContainsKey(ps))
            {
                v = CsaToPiece[ps];
                return true;
            }
            else
            {
                v = Piece.Empty;
                return false;
            }
        }

        public static string Square(int sq)
        {
            if (!(0 <= sq && sq < 81))
            {
                throw new FormatException($"駒の位置が盤面に収まっていません。: {sq}");
            }
            return $"{Core.Square.FileOf(sq) + 1}{Core.Square.RankOf(sq) + 1}";
        }

        public static bool TryParseColor(string s, out Color c)
        {
            if (s == "+" || s == "-")
            {
                c = s == "+" ? Color.Black : Color.White;
                return true;
            }
            else
            {
                c = Color.Black;
                return false;
            }
        }

        public static Color ParseColor(string s)
        {
            if (!TryParseColor(s, out var c))
            {
                throw new FormatException($"CSA 形式ではありません。: {s}");
            }
            return c;
        }
    }
}