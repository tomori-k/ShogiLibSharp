namespace ShogiLibSharp.Core;

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
    Left = 0b000,
    LeftUp = 0b001,
    Up = 0b010,
    RightUp = 0b011,
    Right = 0b100,
    RightDown = 0b101,
    Down = 0b110,
    LeftDown = 0b111,
    None = LeftDown + 1,

    ReverseBit = 0b100,
}

public static class DirectionExtensions
{
    static readonly Direction[] DirectionTable = new Direction[81 * 81];

    static int Index(Square from, Square to)
    {
        return (int)from * 81 + (int)to;
    }

    public static Direction Reverse(this Direction d)
    {
        return d ^ Direction.ReverseBit;
    }

    public static Direction FromTo(Square from, Square to)
    {
        return DirectionTable[Index(from, to)];
    }

    static DirectionExtensions()
    {
        foreach (var i in Squares.All)
        {
            foreach (var j in Squares.All)
            {
                var rank_i = i.Rank();
                var file_i = i.File();
                var rank_j = j.Rank();
                var file_j = j.File();

                if (rank_i < rank_j && file_i == file_j)
                    DirectionTable[Index(i, j)] = Direction.Left;
                else if (rank_i < rank_j && (rank_j - rank_i) == (file_j - file_i))
                    DirectionTable[Index(i, j)] = Direction.LeftUp;
                else if (rank_i == rank_j && file_i < file_j)
                    DirectionTable[Index(i, j)] = Direction.Up;
                else if (rank_i > rank_j && (rank_j - rank_i) == -(file_j - file_i))
                    DirectionTable[Index(i, j)] = Direction.RightUp;
                else if (rank_i > rank_j && file_i == file_j)
                    DirectionTable[Index(i, j)] = Direction.Right;
                else if (rank_i > rank_j && (rank_j - rank_i) == (file_j - file_i))
                    DirectionTable[Index(i, j)] = Direction.RightDown;
                else if (rank_i == rank_j && file_i > file_j)
                    DirectionTable[Index(i, j)] = Direction.Down;
                else if (rank_i < rank_j && (rank_j - rank_i) == -(file_j - file_i))
                    DirectionTable[Index(i, j)] = Direction.LeftDown;
                // 8方向以外 or i==j
                else
                    DirectionTable[Index(i, j)] = Direction.None;
            }
        }
    }
}
