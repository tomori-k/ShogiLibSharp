using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Core
{
    /// <summary>
    /// あるマスから別のマスを見たときの方向           <br/>
    /// 9  8  7  6  5  4  3  2  1                   <br/>
    /// 72 63 54 45 36 27 18 09 00 一               <br/>
    /// 73 64 55 46 37 28 19 10 01 二               <br/>
    /// 74 65 56 47 38 29 20 11 02 三      ↑ RIGHT  <br/>
    /// 75 66 57 48 39 30 21 12 03 四 UP ←   → DOWN <br/>
    /// 76 67 58 49 40 31 22 13 04 五      ↓ LEFT   <br/>
    /// 77 68 59 50 41 32 23 14 05 六               <br/>
    /// 78 69 60 51 42 33 24 15 06 七               <br/>
    /// 79 70 61 52 43 34 25 16 07 八               <br/>
    /// 80 71 62 53 44 35 26 17 08 九               <br/>
    /// </summary>
    public enum Direction
    {
        Left      = 0b000,
        LeftUp    = 0b001,
        Up        = 0b010,
        RightUp   = 0b011,
        Right     = 0b100,
        RightDown = 0b101,
        Down      = 0b110,
        LeftDown  = 0b111,
        None      = LeftDown + 1,

        ReverseBit = 0b100,
    }

    public static class DirectionExtensions
    {
        private static readonly Direction[,] fromToDir = new Direction[81, 81];

        public static Direction Reverse(this Direction d)
        {
            return d ^ Direction.ReverseBit;
        }

        public static Direction FromTo(int from, int to)
        {
            return fromToDir[from, to];
        }

        static DirectionExtensions()
        {
            for (int i = 0; i < 81; ++i)
            {
                for (int j = 0; j < 81; ++j)
                {
                    var rank_i = Square.RankOf(i);
                    var file_i = Square.FileOf(i);
                    var rank_j = Square.RankOf(j);
                    var file_j = Square.FileOf(j);

                    if (rank_i < rank_j && file_i == file_j)
                        fromToDir[i, j] = Direction.Left;
                    else if (rank_i < rank_j && (rank_j - rank_i) == (file_j - file_i))
                        fromToDir[i, j] = Direction.LeftUp;
                    else if (rank_i == rank_j && file_i < file_j)
                        fromToDir[i, j] = Direction.Up;
                    else if (rank_i > rank_j && (rank_j - rank_i) == -(file_j - file_i))
                        fromToDir[i, j] = Direction.RightUp;
                    else if (rank_i > rank_j && file_i == file_j)
                        fromToDir[i, j] = Direction.Right;
                    else if (rank_i > rank_j && (rank_j - rank_i) == (file_j - file_i))
                        fromToDir[i, j] = Direction.RightDown;
                    else if (rank_i == rank_j && file_i > file_j)
                        fromToDir[i, j] = Direction.Down;
                    else if (rank_i < rank_j && (rank_j - rank_i) == -(file_j - file_i))
                        fromToDir[i, j] = Direction.LeftDown;
                    // 8方向以外 or i==j
                    else
                        fromToDir[i, j] = Direction.None;
                }
            }
        }
    }
}
