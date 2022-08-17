using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu
{
    public static class Kif
    {
        public static Kifu Parse(string path, Encoding encoding)
        {
            using var reader = new StreamReader(path, encoding);
            return Parse(reader);
        }

        public static Kifu Parse(TextReader textReader)
        {
            using var reader = new PeekableReader(textReader, '#');
            var info = ParseGameInfo(reader);
            var startpos = ParseStartPos(reader);
            var moveLists = ParseMoveSequences(reader, new Position(startpos));
            return new Kifu(info, startpos, moveLists);
        }

        const string PrefixBlackName = "先手：";
        const string PrefixWhiteName = "後手：";
        const string PrefixStartTime = "開始日時：";
        const string PrefixEndTime = "終了日時：";
        const string PrefixEvent = "棋戦：";
        const string PrefixSite = "場所：";
        const string PrefixOpening = "戦型：";
        const string PrefixHandicap = "手合割：";
        const string PrefixBlackCaps = "先手の持駒：";
        const string PrefixWhiteCaps = "後手の持駒：";
        const string PrefixMultiPv = "変化：";
        const string HeaderMoveSequence = "手数----指手---------消費時間--";

        static readonly DateTimeFormatInfo JpFormat = CultureInfo.GetCultureInfo("ja-JP").DateTimeFormat;

        static void ExpectEqual(string expected, string? actual)
        {
            if (expected != actual)
                throw new FormatException($"棋譜が壊れています。{expected} を予期していた行が {actual ?? "null"} でした。");
        }

        // (.*)：(.*)
        static GameInfo ParseGameInfo(PeekableReader reader)
        {
            var info = new GameInfo();

            while (true)
            {
                var line = reader.PeekLine();

                if (line is null
                    || line.StartsWith(PrefixWhiteCaps)
                    || !line.Contains('：')) break;

                reader.ReadLine();

                if (line.StartsWith(PrefixBlackName))
                {
                    info.Names[0] = line[PrefixBlackName.Length..];
                }
                else if (line.StartsWith(PrefixWhiteName))
                {
                    info.Names[1] = line[PrefixWhiteName.Length..];
                }
                else if (line.StartsWith(PrefixStartTime)
                    && DateTime.TryParse(line[PrefixStartTime.Length..], JpFormat, DateTimeStyles.None, out var startTime))
                {
                    info.StartTime = startTime;
                }
                else if (line.StartsWith(PrefixEndTime)
                    && DateTime.TryParse(line[PrefixEndTime.Length..], JpFormat, DateTimeStyles.None, out var endTime))
                {
                    info.EndTime = endTime;
                }
                else if (line.StartsWith(PrefixEvent))
                {
                    info.Event = line[PrefixEvent.Length..];
                }
                else if (line.StartsWith(PrefixSite))
                {
                    info.Site = line[PrefixSite.Length..];
                }
                else if (line.StartsWith(PrefixOpening))
                {
                    info.Opening = line[PrefixOpening.Length..];
                }
                else if (line.StartsWith(PrefixHandicap))
                {
                    // todo
                    var handicapStr = line[PrefixHandicap.Length..].Trim();
                    if (handicapStr != "平手")
                    {
                        throw new NotImplementedException($"現在、平手以外の棋譜は対応していません。: {handicapStr}");
                    }
                }
            }

            return info;
        }

        static Board ParseStartPos(PeekableReader reader)
        {
            // 開始局面が指定されていなければとりあえず平手
            // todo: 駒落ち対応
            if (reader.PeekLine() is not { } firstLine
                || !firstLine.StartsWith(PrefixWhiteCaps))
                return new Position(Position.Hirate).ToBoard();

            var board = new Board();

            ParseCaptureList(reader, board, Color.White);
            ParseBoard(reader, board);
            ParseCaptureList(reader, board, Color.Black);

            return board;
        }

        static readonly Dictionary<string, Piece> BodPieceTable = new Dictionary<string, Piece>
        {
            { " 歩", Piece.B_Pawn },
            { " 香", Piece.B_Lance },
            { " 桂", Piece.B_Knight },
            { " 銀", Piece.B_Silver },
            { " 金", Piece.B_Gold },
            { " 角", Piece.B_Bishop },
            { " 飛", Piece.B_Rook },
            { " 玉", Piece.B_King },
            { " と", Piece.B_ProPawn },
            { " 杏", Piece.B_ProLance },
            { " 圭", Piece.B_ProKnight },
            { " 全", Piece.B_ProSilver },
            { " 馬", Piece.B_ProBishop },
            { " 龍", Piece.B_ProRook },
            { " 竜", Piece.B_ProRook },
            { "v歩", Piece.W_Pawn },
            { "v香", Piece.W_Lance },
            { "v桂", Piece.W_Knight },
            { "v銀", Piece.W_Silver },
            { "v金", Piece.W_Gold },
            { "v角", Piece.W_Bishop },
            { "v飛", Piece.W_Rook },
            { "v玉", Piece.W_King },
            { "vと", Piece.W_ProPawn },
            { "v杏", Piece.W_ProLance },
            { "v圭", Piece.W_ProKnight },
            { "v全", Piece.W_ProSilver },
            { "v馬", Piece.W_ProBishop },
            { "v龍", Piece.W_ProRook },
            { "v竜", Piece.W_ProRook },
        };

        static readonly Dictionary<string, Piece> PieceTable = new Dictionary<string, Piece>
        {
            { "歩", Piece.Pawn },
            { "香", Piece.Lance },
            { "桂", Piece.Knight },
            { "銀", Piece.Silver },
            { "金", Piece.Gold },
            { "角", Piece.Bishop },
            { "飛", Piece.Rook },
            { "玉", Piece.King },
            { "王", Piece.King },
            { "と", Piece.ProPawn },
            { "杏", Piece.ProLance },
            { "圭", Piece.ProKnight },
            { "全", Piece.ProSilver },
            { "成香", Piece.ProLance },
            { "成桂", Piece.ProKnight },
            { "成銀", Piece.ProSilver },
            { "馬", Piece.ProBishop },
            { "龍", Piece.ProRook },
            { "竜", Piece.ProRook },
        };

        static readonly Dictionary<string, int> KansuujiTable = new Dictionary<string, int>
        {
            { "一", 1 },
            { "二", 2 },
            { "三", 3 },
            { "四", 4 },
            { "五", 5 },
            { "六", 6 },
            { "七", 7 },
            { "八", 8 },
            { "九", 9 },
            { "十", 10 },
            { "十一", 11 },
            { "十二", 12 },
            { "十三", 13 },
            { "十四", 14 },
            { "十五", 15 },
            { "十六", 16 },
            { "十七", 17 },
            { "十八", 18 },
        };

        static readonly Dictionary<char, int> ZenkakuSuujiTable = new Dictionary<char, int>
        {
            { '１', 1 },
            { '２', 2 },
            { '３', 3 },
            { '４', 4 },
            { '５', 5 },
            { '６', 6 },
            { '７', 7 },
            { '８', 8 },
            { '９', 9 },
        };

        static void ParseBoard(PeekableReader reader, Board board)
        {
            ExpectEqual("  ９ ８ ７ ６ ５ ４ ３ ２ １", reader.ReadLine()?.TrimEnd());
            ExpectEqual("+---------------------------+", reader.ReadLine()?.TrimEnd());

            for (int rank = 0; rank < 9; ++rank)
            {
                var line = reader.ReadLine();
                if (line is null || line.Length < 2 * 9 + 1)
                    throw new FormatException("開始局面の情報が壊れています。");

                for (int file = 8; file >= 0; --file)
                {
                    var i = (8 - file) * 2 + 1;
                    var pieceStr = line[i..(i+2)];

                    if (pieceStr == " ・") continue;

                    if (!BodPieceTable.ContainsKey(pieceStr))
                        throw new FormatException($"開始局面の情報が壊れています。行: {line}");

                    board.Squares[Square.Index(rank, file)] = BodPieceTable[pieceStr];
                }
            }

            ExpectEqual("+---------------------------+", reader.ReadLine()?.TrimEnd());
        }

        static void ParseCaptureList(PeekableReader reader, Board board, Color c)
        {
            int i;
            var line = reader.ReadLine();
            if (line is null || (i = line.IndexOf('：')) == -1)
                throw new FormatException("開始局面の情報が壊れています。");

            var captureStr = line[(i + 1)..].TrimEnd();

            if (captureStr == "なし") return;

            var sp = captureStr.Split('　', StringSplitOptions.RemoveEmptyEntries); // 全角スペースで分割

            foreach (var cs in sp)
            {
                var pieceStr = cs[0..1];
                var countStr = cs[1..];

                if (PieceTable.ContainsKey(pieceStr) && (countStr.Length == 0 || KansuujiTable.ContainsKey(countStr)))
                {
                    board.CaptureListOf(c).Add(
                        PieceTable[pieceStr],
                        countStr.Length == 0 ? 1 : KansuujiTable[countStr]);
                }
                else
                {
                    throw new FormatException($"開始局面の情報が壊れています。: {line}");
                }
            }
        }

        
        static List<MoveSequence> ParseMoveSequences(PeekableReader reader, Position pos)
        {
            var moveLists = new List<MoveSequence>();

            while (true)
            {
                moveLists.Add(ParseMoveSequence(reader, pos));

                while (true)
                {
                    if (reader.PeekLine() is not { } line || line.StartsWith("変化"))
                        break;
                    reader.ReadLine();
                }

                if (reader.PeekLine() is null) break;
            }

            return moveLists;
        }

        static readonly string[] SpecialMove = new[]
        {
            "中断", "投了", "持将棋", "千日手", "詰み", "切れ負け", "反則勝ち", "反則負け", "入玉勝ち", "不戦勝", "不戦敗"
        };

        static readonly Regex MoveLineRegex = new(@"^\s*\d+\s+(?<move>.+?)(\s+(?<time>\(.+\)))?$", RegexOptions.Compiled);

        // 手数----指手---------消費時間-- or 変化：n手 <- ここから
        // 指し手1
        // 指し手2
        // ...
        // [まで～ or 空行]                            <- ここまで一つのシーケンス
        // [変化：m手 or 終端]
        // ...

        static MoveSequence ParseMoveSequence(PeekableReader reader, Position pos)
        {
            int startPly;
            {
                var line = reader.ReadLine();

                if (line is null)
                    throw new FormatException("棋譜開始を示す行がありません。");

                if (line.TrimEnd() == HeaderMoveSequence)
                {
                    startPly = 1;
                }
                else if (line.StartsWith(PrefixMultiPv))
                {
                    if (int.TryParse(
                        line[PrefixMultiPv.Length..].TrimEnd('手'),
                        out var ply)
                        && 1 <= ply
                        && ply <= pos.GamePly) startPly = ply;
                    else
                        throw new FormatException($"分岐棋譜の開始手数が壊れています: {line}");
                }
                else
                    throw new FormatException($"棋譜開始を示す行がありません。:{line}");
            }

            // 開始局面に対するコメントを読み飛ばす
            ParseMoveComment(reader);

            // 分岐位置まで巻き戻し
            while (pos.GamePly != startPly)
            {
                pos.UndoMove();
            }

            bool prevIllegal = false;
            var moves = new List<MoveInfo>();
            while (true)
            {
                var line = reader.PeekLine();
                if (line is null || line.StartsWith("変化")) break;

                reader.ReadLine();

                if (line.TrimEnd().Length == 0 || line.StartsWith("まで")) break;

                var match = MoveLineRegex.Match(line);
                if (!match.Success)
                    throw new FormatException($"指し手の情報が壊れています。:{line}");

                var moveStr = match.Groups["move"].Value;

                if (SpecialMove.Contains(moveStr))
                {
                    // todo
                }
                else if (!prevIllegal)
                {
                    if (!match.Groups.ContainsKey("time"))
                        throw new FormatException($"消費時間の情報がありません。: {line}");
                    var move = ParseMove(moveStr, pos);
                    var time = ParseTime(match.Groups["time"].Value);
                    var comment = ParseMoveComment(reader);

                    moves.Add(new MoveInfo(move, time, comment));

                    if (pos.IsLegalMove(move))
                    {
                        pos.DoMove(move);
                    }
                    else prevIllegal = true;
                }
                else
                    throw new FormatException("棋譜の途中に非合法手が含まれています。");
            }

            return new MoveSequence(startPly, moves);
        }

        // [<手番>]<移動先座標><駒>[<装飾子>]<移動元座標> <- 駒打ちのときは from を書いてない棋譜多数、ホムペ嘘つき

        static readonly Regex MoveRegex = new(
            @"^([▲△])?(?<to>([１２３４５６７８９][一二三四五六七八九])|同　)(?<piece>成[銀桂香]|[王玉金銀全桂圭香杏角馬飛龍竜歩と])(?<motion>[打成])?(\((?<from>[1-9]{2})\))?$", RegexOptions.Compiled);
        const string SameTo = "同　";

        static Move ParseMove(string s, Position pos)
        {
            var match = MoveRegex.Match(s);
            if (!match.Success) throw new FormatException($"指し手の情報が壊れています: {s}");

            var toStr = match.Groups["to"].Value;
            var motion = match.Groups["motion"].Value;
            if (motion == "打")
            {
                var pieceStr = match.Groups["piece"].Value;

                if (toStr == SameTo || !PieceTable.ContainsKey(pieceStr))
                {
                    throw new FormatException($"指し手の情報が壊れています: {s}");
                }

                var to = ParseToSquare(toStr);
                return MoveExtensions.MakeDrop(PieceTable[pieceStr], to);
            }
            else
            {
                if (toStr == SameTo && pos.GamePly < 2)
                {
                    throw new FormatException($"直前の指し手が存在しません: {s}");
                }
                var from = ParseFromSquare(match.Groups["from"].Value);
                var to = toStr == SameTo ? pos.LastMove.To() : ParseToSquare(toStr);
                var promote = motion == "成";
                return MoveExtensions.MakeMove(from, to, promote);
            }
        }

        static int ParseFromSquare(string s)
        {
            return Square.Index(s[1] - '1', s[0] - '1');
        }

        static int ParseToSquare(string s)
        {
            return Square.Index(KansuujiTable[s[1..]] - 1, ZenkakuSuujiTable[s[0]] - 1);
        }

        static readonly Regex TimeRegex = new(@"^\(\s*(\d+):(\d+)/(\d+):(\d+):(\d+)\s*\)$", RegexOptions.Compiled);

        static TimeSpan ParseTime(string s)
        {
            var match = TimeRegex.Match(s);
            if (!match.Success) throw new FormatException($"消費時間の情報が壊れています:{s}");

            var minutes = int.Parse(match.Groups[1].Value);
            var seconds = int.Parse(match.Groups[2].Value);

            return new TimeSpan(hours: 0, minutes: minutes, seconds: seconds);
        }

        static string ParseMoveComment(PeekableReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                if (reader.PeekLine() is not { } line
                    || !line.StartsWith('*')) break;
                reader.ReadLine();
                sb.AppendLine(line[1..]);
            }
            return sb.ToString();
        }
    }
}

