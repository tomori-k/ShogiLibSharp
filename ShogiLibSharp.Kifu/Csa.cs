using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu
{
    public static class Csa
    {
        // CSA 棋譜ファイル形式：http://www2.computer-shogi.org/protocol/record_v22.html

        public static Kifu ParseKifu(string path)
        {
            using var reader = new StreamReader(path);
            return ParseKifu(reader);
        }

        /// <summary>
        /// CSA 形式の棋譜をパース
        /// </summary>
        /// <exception cref="FormatException"></exception>
        public static Kifu ParseKifu(TextReader textReader)
        {
            using var reader = new PeekableReader(textReader);
            ParseVersion(reader);
            var (nameBlack, nameWhite) = ParseNames(reader);
            var info = ParseGameInfo(reader);
            var startpos = Core.Csa.ParseStartPosition(reader);
            var moves = ParseMoves(reader, startpos);
            // ParseResult(lines);
            return new Kifu(info, startpos, new() { moves });
        }

        static void ParseVersion(PeekableReader reader)
        {
            var version = reader.ReadLine();
            if (version != "V2.2")
            {
                throw new FormatException("V2.2以外のバージョンのフォーマットはサポートしていません");
            }
        }

        const string PrefixBlack = "N+";
        const string PrefixWhite = "N-";

        /// <summary>
        /// CSA 形式の棋譜の対局者情報をパース
        /// </summary>
        static (string?, string?) ParseNames(PeekableReader reader)
        {
            string? nameBlack = null;
            string? nameWhite = null;
            if (reader.PeekLine() is { } b && b.StartsWith(PrefixBlack))
            {
                nameBlack = b[PrefixBlack.Length..];
                reader.ReadLine();
            }
            if (reader.PeekLine() is { } w && w.StartsWith(PrefixWhite))
            {
                nameWhite = w[PrefixWhite.Length..];
                reader.ReadLine();
            }
            return (nameBlack, nameWhite);
        }

        const string PrefixEvent = "$EVENT:";
        const string PrefixSite = "$SITE:";
        const string PrefixStartTime = "$START_TIME:";
        const string PrefixEndTime = "$END_TIME:";
        const string PrefixTimeLimit = "$TIME_LIMIT:";
        const string PrefixOpening = "$OPENING:";

        /// <summary>
        /// CSA 形式の棋譜の棋譜情報をパース
        /// </summary>
        static GameInfo ParseGameInfo(PeekableReader reader)
        {
            var info = new GameInfo();
            while (true)
            {
                var line = reader.PeekLine();
                if (line is null || !line.StartsWith("$")) break;

                reader.ReadLine();

                if (line.StartsWith(PrefixEvent))
                {
                    info.Event = line[PrefixEvent.Length..];
                }
                else if (line.StartsWith(PrefixSite))
                {
                    info.Site = line[PrefixSite.Length..];
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
                else if (line.StartsWith(PrefixTimeLimit))
                {
                    // todo
                }
                else if (line.StartsWith(PrefixOpening))
                {
                    info.Opening = line[PrefixOpening.Length..];
                }
            }
            return info;
        }

        /// <summary>
        /// CSA 形式の棋譜の指し手・消費時間をパース
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startpos"></param>
        /// <returns></returns>
        static List<MoveInfo> ParseMoves(PeekableReader reader, Board startpos)
        {
            var pos = new Position(startpos);
            var moves = new List<MoveInfo>();

            while (true)
            {
                var moveStr = reader.ReadLine();
                if (moveStr is null
                    || !(moveStr.StartsWith("+") || moveStr.StartsWith("-"))) break;

                var move = Core.Csa.ParseMove(moveStr, pos);
                pos.DoMove(move);

                // 消費時間はオプションなので、ないこともある

                if (reader.PeekLine() is { } timeStr && timeStr.StartsWith("T"))
                {
                    reader.ReadLine();
                    moves.Add(new(move, Core.Csa.ParseTime(timeStr)));
                }
                else 
                    moves.Add(new(move, null));
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
    }
}