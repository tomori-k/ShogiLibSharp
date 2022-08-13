using ShogiLibSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Kifu
{
    public enum Handicap
    {

    }

    public record MoveInfo(Move Move, TimeSpan? Elapsed, string? Comment = null);

    public class GameInfo
    {
        public Handicap? Handicap { get; set; }
        public string?[] Names { get; } = new string?[2];
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Event { get; set; }
        public string? Opening { get; set; }
        public string? Site { get; set; }
    }

    public class Kifu
    {
        public GameInfo GameInfo { get; set; }
        public Board StartPos { get; set; }
        public List<List<MoveInfo>> MoveLists { get; set; }

        public Kifu(GameInfo info, Board startpos, List<List<MoveInfo>> moveLists)
        {
            (GameInfo, StartPos, MoveLists) = (info, startpos, moveLists);
        }
    }
}
