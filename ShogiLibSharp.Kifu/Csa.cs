using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu
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
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !x.StartsWith("'"))
                .Select(x => x.Split(','))
                .SelectMany(x => x));
            {
                var version = lines.Dequeue();
                if (version != "V2.2")
                {
                    throw new FormatException("V2.2以外のバージョンのフォーマットはサポートしていません");
                }
            }
            ParseNames(lines);
            ParseGameInfo(lines);
            var startpos = Core.Csa.ParseStartPosition(lines);
            var moves = Core.Csa.ParseMoves(lines, startpos);
            // ParseResult(lines);
            return (startpos.Sfen(), moves.Select(x => x.Item1).ToList());
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