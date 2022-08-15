using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu
{
    public static class Kif
    {
        public static Kifu Parse(string path)
        {
            using var reader = new StreamReader(path);
            return Parse(reader);
        }

        public static Kifu Parse(TextReader textReader)
        {
            using var reader = new PeekableReader(textReader, commentPrefix: '#');
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

        static GameInfo ParseGameInfo(PeekableReader reader)
        {
            var info = new GameInfo();

            while (true)
            {
                var line = reader.PeekLine();

                if (line is null
                    || line.StartsWith(PrefixWhiteCaps)
                    || line == HeaderMoveSequence) break;

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
                    && DateTime.TryParse(line[PrefixStartTime.Length..], out var startTime))
                {
                    info.StartTime = startTime;
                }
                else if (line.StartsWith(PrefixEndTime)
                    && DateTime.TryParse(line[PrefixEndTime.Length..], out var endTime))
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
            reader.ReadLine(); // ９ ８...
            reader.ReadLine(); // +----...

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

            reader.ReadLine(); // +----...
        }

        static void ParseCaptureList(PeekableReader reader, Board board, Color c)
        {
            int i;
            var line = reader.ReadLine();
            if (line is null || (i = line.IndexOf('：')) == -1)
                throw new FormatException("開始局面の情報が壊れています。");

            var sp = line[(i + 1)..].Split('　', StringSplitOptions.RemoveEmptyEntries); // 全角スペースで分割
            foreach (var cs in sp)
            {
                var pieceStr = cs[0..1];
                var countStr = cs[1..];

                if (PieceTable.ContainsKey(pieceStr) && (countStr.Length == 0 || KansuujiTable.ContainsKey(countStr)))
                {
                    board.CaptureListOf(c).Add(
                        PieceTable[pieceStr],
                        countStr.Length == 0 ? 0 : KansuujiTable[countStr]);
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
                    if (reader.PeekLine() is not { } line || line.Length > 0) break;
                    reader.ReadLine();
                }

                if (reader.PeekLine() is not { } nextLine
                    || !nextLine.StartsWith("変化")) break;

            }

            return moveLists;
        }

        static readonly string[] SpecialMove = new[]
        {
            "中断", "投了", "持将棋", "千日手", "詰み", "切れ負け", "反則勝ち", "反則負け", "入玉勝ち", "不戦勝", "不戦敗"
        };

        static MoveSequence ParseMoveSequence(PeekableReader reader, Position pos)
        {
            // 手数----指手---------消費時間-- or 変化：n手 までを読み飛ばす
            int startPly = 1;
            while (true)
            {
                if (reader.ReadLine() is not { } line
                    || line == HeaderMoveSequence) break;

                if (line.StartsWith(PrefixMultiPv))
                {
                    if (int.TryParse(
                        line[PrefixMultiPv.Length..].TrimEnd('手'),
                        out var ply)
                        && 1 <= ply
                        && ply <= pos.GamePly) startPly = ply;
                    else
                        throw new FormatException($"分岐棋譜の開始手数が壊れています: {line}");
                    break;
                }
            }

            // 開始局面に対するコメントを読み飛ばす
            ParseMoveComment(reader);

            // 分岐位置まで巻き戻し
            while (pos.GamePly != startPly)
            {
                pos.UndoMove();
            }

            var moves = new List<MoveInfo>();
            while (true)
            {
                if (reader.PeekLine() is not { } line
                    || line.StartsWith("まで")) break;

                reader.ReadLine();

                var sp = line.Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
                if (sp.Length < 2) throw new FormatException($"指し手の情報が欠けています: {line}");

                if (SpecialMove.Contains(sp[1]))
                {
                    // todo
                }
                else
                {
                    var (move, time) = ParseMoveWithTime(line, sp, pos);
                    var comment = ParseMoveComment(reader);
                    moves.Add(new MoveInfo(move, time, comment));
                    pos.DoMove(move);
                }
            }

            return new MoveSequence(startPly, moves);
        }

        static (Move, TimeSpan) ParseMoveWithTime(string line, string[] sp, Position pos)
        {
            if (sp.Length < 3) throw new FormatException($"指し手の情報が欠けています: {line}");

            var move = ParseMove(sp[1], pos);
            var elapsed = ParseTime(sp[2]);
            return (move, elapsed);
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

        static TimeSpan ParseTime(string s)
        {
            var i = s.IndexOf('/');
            if (i != -1 && s.StartsWith('('))
            {
                var timeStr = s[1..i];
                if (TimeSpan.TryParse(timeStr, out var time)) return time;
            }
            throw new FormatException($"消費時間の情報が壊れています:{s}");
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

