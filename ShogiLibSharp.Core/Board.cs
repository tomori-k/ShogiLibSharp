using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace ShogiLibSharp.Core
{
    /// <summary>
    /// 盤面
    /// </summary>
    public class Board
    {
        public Color Player { get; set; }
        public Piece[] Squares { get; } = new Piece[81];
        public CaptureList[] CaptureLists { get; } = new CaptureList[2];

        public Board() { }

        public Board(Board board)
        {
            Player = board.Player;
            Squares = (Piece[])board.Squares.Clone();
            CaptureLists = new[] { board.CaptureLists[0], board.CaptureLists[1] };
        }

        /// <summary>
        /// c の駒台
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public ref CaptureList CaptureListOf(Color c)
        {
            return ref CaptureLists[(int)c];
        }

        public Board Clone()
        {
            return new Board(this);
        }

        /// <summary>
        /// ゼロクリア
        /// </summary>
        public void Clear()
        {
            Squares.AsSpan().Fill(Piece.Empty);
            CaptureLists[0].Clear();
            CaptureLists[1].Clear();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Board b) return false;
            if (b.Player != Player) return false;
            if (!b.Squares.SequenceEqual(Squares)) return false;
            if (!(b.CaptureLists[0].Equals(this.CaptureLists[0])
                && b.CaptureLists[1].Equals(this.CaptureLists[1]))) return false;
            return true;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        private static readonly int[] defaultCount = { 0, 18, 4, 4, 4, 4, 2, 2 };

        /// <summary>
        /// 以下をチェックし、満たさない場合 FormatException を発生 <br/>
        /// * 各種類の駒の数がデフォルト以下 <br/>
        /// * 先手、後手の玉が1枚ずつ存在  <br/>
        /// * 二歩でない                   <br/>
        /// * 歩、香が 1 段目にいない       <br/>
        /// * 桂が １,2 段目にいない        <br/>
        /// </summary>
        /// <exception cref="FormatException"></exception>
        public void Validate()
        {
            // 各種類の駒の数がデフォルト以下
            foreach (Piece p in PieceExtensions.PawnToRook)
            {
                int bc = Squares.Where(x => x.Kind() == p).Count();
                int cc = CaptureListOf(Color.Black).Count(p) + CaptureListOf(Color.White).Count(p);
                if (bc + cc > defaultCount[(int)p])
                {
                    throw new FormatException($"{p}が{defaultCount[(int)p]}より多いです。");
                }
            }
            // 先手、後手の玉が1枚ずつ存在
            foreach (Color c in Enum.GetValues(typeof(Color)))
            {
                if (Squares
                    .Where(x => x == Piece.King.Colored(c))
                    .Count() != 1)
                {
                    throw new FormatException($"{c}の玉が1枚でないです。");
                }
            }
            // 二歩でない
            foreach (Color c in Enum.GetValues(typeof(Color)))
            {
                for (int file = 0; file < 9; ++file)
                {
                    if (Squares
                        .Where((p, sq) => p == Piece.Pawn.Colored(c) && Square.FileOf(sq) == file)
                        .Count() >= 2)
                    {
                        throw new FormatException($"{file+1}筋に歩が2枚以上あります。");
                    }
                }
            }
            // 歩が1段目にいない
            foreach (Color c in Enum.GetValues(typeof(Color)))
            {
                if (Squares
                    .Where((p, sq) => p == Piece.Pawn.Colored(c) && Square.RankOf(c, sq) == 0)
                    .Any())
                {
                    throw new FormatException($"1段目に歩が存在します。");
                }
            }
            // 香が1段目にいない
            foreach (Color c in Enum.GetValues(typeof(Color)))
            {
                if (Squares
                    .Where((p, sq) => p == Piece.Lance.Colored(c) && Square.RankOf(c, sq) == 0)
                    .Any())
                {
                    throw new FormatException($"1段目に香車が存在します。");
                }
            }
            // 桂が1,2段目にいない
            foreach (Color c in Enum.GetValues(typeof(Color)))
            {
                if (Squares
                    .Where((p, sq) => p == Piece.Knight.Colored(c) && Square.RankOf(c, sq) <= 1)
                    .Any())
                {
                    throw new FormatException($"1,2段目に桂馬が存在します。");
                }
            }
        }

        /// <summary>
        /// 人が見やすい文字列に変換
        /// </summary>
        /// <returns></returns>
        public string Pretty()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"手番：{Player.Pretty()}");
            sb.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １");
            sb.AppendLine("+-------------------------+");

            for (int rank = 0; rank < 9; ++rank)
            {
                sb.Append("|");

                for (int file = 8; file >= 0; --file)
                    sb.Append($"{Squares[Square.Index(rank, file)].Pretty(),2}");

                sb.AppendLine($"|{Square.PrettyRank(rank)}");
            }

            sb.AppendLine("+-------------------------+");
            sb.AppendLine($"先手持ち駒：{CaptureListOf(Color.Black).Pretty()}");
            sb.AppendLine($"後手持ち駒：{CaptureListOf(Color.White).Pretty()}");

            return sb.ToString();
        }
    }
}
