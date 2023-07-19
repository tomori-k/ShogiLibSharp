using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu
{
    // CSA 棋譜ファイル形式：http://www2.computer-shogi.org/protocol/record_v22.html

    public record Csa
    {
        public string? NameBlack { get; set; }
        public string? NameWhite { get; set; }
        public string? Event { get; set; }
        public string? Site { get; set; }
        public string? Opening { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeLimit { get; set; }
        public TimeSpan Byoyomi { get; set; }
        public Position StartPos { get; set; } = new();
        public List<CsaMove> Moves { get; set; } = new();

        public static Csa Parse(TextReader textReader)
        {
            var csa = new Csa();

            csa.ParseNames(textReader);
        }

        void ParseNames(TextReader textReader)
        {
            const string PrefixBlack = "N+";
            const string PrefixWhite = "N-";

            while (textReader.Peek() == 'N')
            {
                var line = textReader.ReadLine();

                if (line.StartsWith(PrefixBlack))
                {
                    this.NameBlack = line[PrefixBlack.Length..];
                }
                else if (line.StartsWith(PrefixWhite))
                {
                    this.NameWhite = line[PrefixWhite.Length..];
                }
                else
                {
                    ThrowFormatException(line);
                }
            }
        }

        void ParseInfo(TextReader textReader)
        {
            const string PrefixEvent = "$EVENT:";
            const string PrefixSite = "$SITE:";
            const string PrefixStartTime = "$START_TIME:";
            const string PrefixEndTime = "$END_TIME:";
            const string PrefixTimeLimit = "$TIME_LIMIT:";
            const string PrefixOpening = "$OPENING:";
            const string TimeFormat = "yyyy/MM/dd hh:mm:ss";
            const string TimeLimitFormat = "hh:mm+ss";

            while (textReader.Peek() == '$')
            {
                var line = reader.ReadLine();

                if (line.StartsWith(PrefixEvent))
                {
                    this.Event = line[PrefixEvent.Length..];
                }
                else if (line.StartsWith(PrefixSite))
                {
                    this.Site = line[PrefixSite.Length..];
                }
                else if (line.StartsWith(PrefixStartTime))
                {
                    var time = line[PrefixStartTime.Length..];

                    if (DateTime.TryParseExact(time, TimeFormat, new CultureInfo("ja-JP"), DateTimeStyles.None, out var startTime))
                    {
                        this.StartTime = time;
                    }
                }
                else if (line.StartsWith(PrefixEndTime))
                {
                    var time = line[PrefixEndTime.Length..];

                    if (DateTime.TryParseExact(time, TimeFormat, new CultureInfo("ja-JP"), DateTimeStyles.None, out var endTime))
                    {
                        this.EndTime = endTime;
                    }
                }
                else if (line.StartsWith(PrefixTimeLimit))
                {
                    var timeLimitStr = line[PrefixTimeLimit.Length..];

                    if (TimeSpan.TryParseExact(timeLimitStr, TimeLimitFormat, new CultureInfo("ja-JP"), TimeSpanStyles.None, out var timeLimit))
                    {
                        this.Byoyomi = TimeSpan.FromSeconds(timeLimit.Seconds);
                        this.TimeLimit = timeLimit - this.Byoyomi;
                    }
                }
                else if (line.StartsWith(PrefixOpening))
                {
                    this.Opening = line[PrefixOpening.Length..];
                }
            }
        }

        static Piece ParsePiece2(ReadOnlySpan<char> slice)
        {
            return slice switch
            {
                "FU" => Piece.Pawn,
                "KY" => Piece.Lance,
                "KE" => Piece.Knight,
                "GI" => Piece.Silver,
                "KI" => Piece.Gold,
                "KA" => Piece.Bishop,
                "HI" => Piece.Rook,
                "OU" => Piece.King,
                "TO" => Piece.ProPawn,
                "NY" => Piece.ProLance,
                "NK" => Piece.ProKnight,
                "NG" => Piece.ProSilver,
                "UM" => Piece.ProBishop,
                "RY" => Piece.ProRook,
                _ => throw new FormatException(), // todo
            };
        }

        static Piece ParsePiece3(ReadOnlySpan<char> slice)
        {
            return slice switch
            {
                " * " => Piece.Empty,
                "+FU" => Piece.B_Pawn,
                "+KY" => Piece.B_Lance,
                "+KE" => Piece.B_Knight,
                "+GI" => Piece.B_Silver,
                "+KI" => Piece.B_Gold,
                "+KA" => Piece.B_Bishop,
                "+HI" => Piece.B_Rook,
                "+OU" => Piece.B_King,
                "+TO" => Piece.B_ProPawn,
                "+NY" => Piece.B_ProLance,
                "+NK" => Piece.B_ProKnight,
                "+NG" => Piece.B_ProSilver,
                "+UM" => Piece.B_ProBishop,
                "+RY" => Piece.B_ProRook,
                "-FU" => Piece.W_Pawn,
                "-KY" => Piece.W_Lance,
                "-KE" => Piece.W_Knight,
                "-GI" => Piece.W_Silver,
                "-KI" => Piece.W_Gold,
                "-KA" => Piece.W_Bishop,
                "-HI" => Piece.W_Rook,
                "-OU" => Piece.W_King,
                "-TO" => Piece.W_ProPawn,
                "-NY" => Piece.W_ProLance,
                "-NK" => Piece.W_ProKnight,
                "-NG" => Piece.W_ProSilver,
                "-UM" => Piece.W_ProBishop,
                "-RY" => Piece.W_ProRook,
                _ => throw new FormatException(), // todo
            };
        }

        static Square ParseSquare(ReadOnlySpan<char> slice)
        {
            var rank = FromOneToNine(slice[1]) ? (Rank)(slice[1] - '1') : throw new FormatException();
            var file = FromOneToNine(slice[0]) ? (File)(slice[1] - '1') : throw new FormatException();

            return Squares.Index(rank, file);
        }

        static Move ParseMove(ReadOnlySpan<char> slice, Position pos)
        {
            // 駒打ち
            if (buffer[..2] is "00")
            {
                var to = ParseSquare(buffer[2..4]);
                var drop = ParsePiece2(buffer[4..6]);
                return MoveExtensions.MakeDrop(to, drop);
            }
            else
            {
                var from = ParseSquare(slice);
                var to = ParseSquare(slice[2..4]);
                var after = ParsePiece2(slice[4..6]);
                var promote = !pos[from].IsPromoted() && after.IsPromoted();

                return MoveExtensions.MakeMove(from, to, promote);
            }
        }

        static bool FromOneToNine(int c)
        {
            return '1' <= c && c <= '9';
        }

        static void ReadUntilNextLine(TextReader textReader)
        {
            while (textReader.Peek() != '\n' /* LF or CRLF のどちらにせよ、LF=\n が来るまで読み飛ばせばよい*/)
            {
                textReader.Read();
            }

            // 最後に \n を読み飛ばす
            textReader.Read();
        }

        void ParsePosition(TextReader textReader)
        {
            while (textReader.Peek() == 'P')
            {
                textReader.Read(); // 'P' 読み捨て

                var next = textReader.Read();

                if (FromOneToNine(next))
                {
                    var rank = (Rank)(next - '1');
                    var file = File.F9;
                    Span<char> buffer = stackalloc char[3];

                    while (textReader.Peek() is '+' or '-' or ' ')
                    {
                        if (textReader.ReadBlock(buffer) < 3)
                        {
                            throw new FormatException(); // todo: 行番号
                        }

                        if (file < File.F1)
                        {
                            throw new FormatException();
                        }

                        this.StartPos._pieces[Squares.Index(rank, file)] = ParsePiece3(buffer);
                        --file;
                    }
                }
                else if (next is '+' or '-')
                {
                    var c = next == '+' ? Color.Black : Color.White;
                    Span<char> buffer = stackalloc char[4];

                    while ('1' <= textReader.Peek() && textReader.Peek() <= '9')
                    {
                        if (textReader.ReadBlock(buffer) < 4)
                        {
                            throw new FormatException();
                        }

                        var piece = ParsePiece2(buffer[2..]);

                        // 持ち駒
                        if (buffer[..2] is "00")
                        {
                            this.StartPos._hands[(int)c].Add(piece, 1);
                        }
                        // 盤上の駒
                        else
                        {
                            this.StartPos._pieces[ParseSquare(buffer)] = piece.Colored(c);
                        }
                    }
                }
                else
                {
                    throw new FormatException();
                }

                ReadUntilNextLine();
            }

            // 手番

            var next = textReader.Read();
            var c = next == '+' ? Color.Black
                : next == '-' ? Color.White
                : throw new FormatException();

            this.StartPos.Player = c;

            // set internal state
        }

        void ParseMoves(TextReader textReader)
        {
            var pos = new Position(this.StartPos);

            while (textReader.Peek() is '+' or '-' or '%' or 'T')
            {
                var next = textReader.Read();

                if (next == 'T') 
                {
                    if (this.Moves.Count == 0)
                    {
                        throw new FormatException();
                    }

                    var sec = 0;

                    while (textReader.Peek() is >= '0' and <= '9')
                    {
                        sec *= 10;
                        sec += textReader.Read() - '0';
                    }

                    this.Moves.Last().Elapsed = TimeSpan.FromSeconds(sec);
                }
                else
                {
                    var moveStr = textReader.ReadLine();

                    this.Moves.Add(new(moveStr));
                    // var c = next == '+' ? Color.Black : Color.White;

                    // if (pos.Player != c)
                    // {
                    //     throw new Exception(); // 寄付の手番がおかしい！
                    // }

                    // Span<char> buffer = stackalloc char[6];

                    // if (textReader.ReadBlock(buffer) < 6)
                    // {
                    //     throw new FormatException();
                    // }

                    // var move = ParseMove(buffer, pos);

                    // pos.DoMove(move);
                }
            }
        }

        void ParseResult(TextReader textReader)
        {
            
        }

        static void ThrowFormatException(string line)
        {
            throw new FormatException($"予期しない形式の行です: {line}");
        }
    }

    public record CsaMove(string MoveStr)
    {
        public string? Comment { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}