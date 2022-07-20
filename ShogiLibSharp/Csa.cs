namespace ShogiLibSharp
{
    public static class Csa
    {
        // CSA 棋譜ファイル形式：http://www2.computer-shogi.org/protocol/record_v22.html

        /// <summary>
        /// CSA 形式の棋譜をパース
        /// </summary>
        /// <param name="csaKifu"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static (string, List<Move>) ParseKifu(string csaKifu)
        {
            var lines = new Queue<string>(csaKifu
                .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !x.StartsWith("'"))
            );
            {
                var version = lines.Dequeue();
                if (version != "V2.2")
                {
                    throw new FormatException("V2.2以外のバージョンのフォーマットはサポートしていません");
                }
            }
            ParseNames(lines);
            ParseGameInfo(lines);
            var initPos = ParsePosition(lines).Sfen();
            var moves = ParseMoves(lines, initPos);
            // ParseResult(lines);
            return (initPos, moves);
        }

        /// <summary>
        /// CSA 形式の棋譜の対局者情報をパース
        /// </summary>
        /// <param name="lines"></param>
        public static void ParseNames(Queue<string> lines)
        {
            if (lines.TryPeek(out var nameBlack) && nameBlack.StartsWith("N+"))
            {
                // todo
                lines.Dequeue();
            }
            if (lines.TryPeek(out var nameWhite) && nameWhite.StartsWith("N-"))
            {
                // todo
                lines.Dequeue();
            }
        }

        /// <summary>
        /// CSA 形式の棋譜の棋譜情報をパース
        /// </summary>
        /// <param name="lines"></param>
        public static void ParseGameInfo(Queue<string> lines)
        {
            while (lines.Count > 0)
            {
                var next = lines.Peek();
                if (!next.StartsWith("$")) break;
                lines.Dequeue();
                if (next.StartsWith("$EVENT:"))
                {
                    // todo
                }
                if (next.StartsWith("$SITE:"))
                {
                    // todo
                }
                if (next.StartsWith("$START_TIME:"))
                {
                    // todo
                }
                if (next.StartsWith("$END_TIME:"))
                {
                    // todo
                    lines.Dequeue();
                }
                if (next.StartsWith("$TIME_LIMIT:"))
                {
                    // todo
                }
                if (next.StartsWith("$OPENING:"))
                {
                    // todo
                }
            }
        }

        /// <summary>
        /// CSA 形式の棋譜の開始局面をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="FormatException"></exception>
        public static Position ParsePosition(Queue<string> lines)
        {
            var board = new Board();
            if (lines.Peek().StartsWith("PI"))
            {
                // todo
                throw new NotImplementedException();
            }
            else
            {
                // 一括表現
                for (int rank = 0; rank < 9; ++rank)
                {
                    var line = lines.Dequeue();
                    if (line.Length < 3 * 9 + 2)
                    {
                        throw new FormatException($"盤面情報が欠けています：{line}");
                    }
                    for (int file = 0; file < 9; ++file)
                    {
                        var ps = line.Substring(2 + (8 - file) * 3, 3);
                        if (ps != " * ")
                        {
                            board.Squares[Square.Index(rank, file)] = ParsePiece(ps);
                        }
                    }
                }
                // 駒別単独表現
                while (lines.Count > 0)
                {
                    var next = lines.Peek();
                    if (!next.StartsWith("P")) break;
                    lines.Dequeue();
                    var pc = next.StartsWith("P+")
                        ? Color.Black : Color.White;
                    for (int i = 2; i + 4 < next.Length; i += 4)
                    {
                        var squareStr = next.Substring(i, 2);
                        var pieceStr = next.Substring(i + 2, 2);
                        // todo: AL対応
                        if (squareStr == "00")
                        {
                            board.CaptureListOf(pc)
                                .Add(ParsePiece(pieceStr), 1);
                        }
                        else
                        {
                            board.Squares[ParseSquare(squareStr)]
                                = ParsePiece(pc == Color.Black ? $"+{pieceStr}" : $"-{pieceStr}");
                        }
                    }
                }
            }
            // 手番
            board.Player = lines.Dequeue() == "+" ? Color.Black : Color.White;
            return new Position(board);
        }

        /// <summary>
        /// CSA 形式の棋譜の指し手・消費時間をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="initPos"></param>
        /// <returns></returns>
        public static List<Move> ParseMoves(Queue<string> lines, string initPos)
        {
            var pos = new Position(initPos);
            var moves = new List<Move>();
            while (lines.Count > 0)
            {
                var moveStr = lines.Dequeue();
                if (!(moveStr.StartsWith("+") || moveStr.StartsWith("-"))) break;
                var move = ParseMove(moveStr, pos);
                moves.Add(move);
                pos.DoMove(move);
                if (lines.TryPeek(out var time) && time.StartsWith("T"))
                {
                    // todo: ParseTime()
                }
            }
            return moves;
        }

        /// <summary>
        /// CSA 形式の棋譜の終局状況をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void ParseResult(Queue<string> lines)
        {
            throw new NotImplementedException();
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
            if (moveStr.Length < 7)
            {
                throw new FormatException($"CSA 形式の指し手文字列ではありません：{moveStr}");
            }
            var to = ParseSquare(moveStr.Substring(3, 2));
            // 駒打ち
            if (moveStr.Substring(1, 2) == "00")
            {
                var pieceStr = moveStr.Substring(5, 2);
                return MoveExtensions.MakeDrop(ParsePiece(pieceStr), to);
            }
            else
            {
                var from = ParseSquare(moveStr.Substring(1, 2));
                var komaAfterStr = moveStr[0] + moveStr.Substring(5, 2);
                var komaAfter = ParsePiece(komaAfterStr);
                var promote = !pos.PieceAt(from).IsPromoted() && komaAfter.IsPromoted();
                return MoveExtensions.MakeMove(from, to, promote);
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
            if (squareStr.Length < 2
                || !('1' <= squareStr[0] && squareStr[0] <= '9')
                || !('1' <= squareStr[1] && squareStr[1] <= '9'))
            {
                throw new FormatException($"CSA 形式の座標文字列ではありません：{squareStr}");
            }
            return Square.Index(squareStr[1] - '1', squareStr[0] - '1');
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
            if (!CsaToPiece.ContainsKey(ps))
            {
                throw new FormatException($"CSA 形式の駒文字列ではありません：{ps}");
            }
            return CsaToPiece[ps];
        }
    }
}