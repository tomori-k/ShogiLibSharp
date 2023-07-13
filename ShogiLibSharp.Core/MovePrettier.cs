//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;

//namespace ShogiLibSharp.Core
//{
//    public static class MovePrettier
//    {
//        private static readonly string[] PrettierPiece
//            = { "??", "歩", "香", "桂", "銀", "金", "角", "飛", "玉", "と", "成香", "成桂", "成銀", "成金", "馬", "龍"};

//        public static string PrettyPiece(Piece p)
//        {
//            return PrettierPiece[(int)p];
//        }

//        /// <summary>
//        /// 駒を移動させる方向
//        /// </summary>
//        private enum MoveDirection
//        {
//            Agaru, Yoru, Hiku
//        }

//        private static readonly string[] PrettyMoveDirection = { "上", "寄", "引" };

//        /// <summary>
//        /// 移動させた駒の、同種間での相対位置
//        /// </summary>
//        private enum Relative
//        {
//            Hidari, Sugu, Migi, SuguBack /* 前に進む「直」と後ろに進む「直」を区別する必要がある */
//        }

//        // SuguBack 用のエントリはいらない
//        // 理由は下記コード参照
//        private static readonly string[] PrettyRelative = { "左", "直", "右" };

//        /// <summary>
//        /// MoveDirections[c,d] = c 視点で、d 方向へ移動する指し手の MoveDirection
//        /// </summary>
//        private static readonly MoveDirection[,] MoveDirections = {
//            { MoveDirection.Hiku, MoveDirection.Hiku, MoveDirection.Yoru, MoveDirection.Agaru,
//              MoveDirection.Agaru, MoveDirection.Agaru, MoveDirection.Yoru, MoveDirection.Hiku },
//            { MoveDirection.Agaru, MoveDirection.Agaru, MoveDirection.Yoru, MoveDirection.Hiku,
//              MoveDirection.Hiku, MoveDirection.Hiku, MoveDirection.Yoru, MoveDirection.Agaru },
//        };

//        /// <summary>
//        /// Relatives[c, d] = c 視点で、d 方向へ移動する指し手で移動させた駒の Relative
//        /// </summary>
//        private static readonly Relative[,] Relatives = {
//            { Relative.SuguBack, Relative.Hidari, Relative.Hidari, Relative.Hidari,
//              Relative.Sugu, Relative.Migi, Relative.Migi, Relative.Migi, },
//            { Relative.Sugu, Relative.Migi, Relative.Migi, Relative.Migi,
//              Relative.SuguBack, Relative.Hidari, Relative.Hidari, Relative.Hidari, },
//        };


//        private static MoveDirection ToMoveDirection(this Direction d, Color c)
//        {
//            return MoveDirections[(int)c, (int)d];
//        }

//        private static string Pretty(this MoveDirection md)
//        {
//            return PrettyMoveDirection[(int)md];
//        }

//        private static Relative ToRelative(this Direction d, Color c)
//        {
//            return Relatives[(int)c, (int)d];
//        }

//        private static string Pretty(this Relative r)
//        {
//            return PrettyRelative[(int)r];
//        }

//        public static string Pretty(Move m, Position pos, bool isRecapture)
//        {
//            var prettyPlayer = pos.Player
//                .Pretty(ColorExtensions.PrettyType.Triangle);

//            if (m == Move.Resign)
//            {
//                return $"{prettyPlayer}投了";
//            }
//            else if (m == Move.Win)
//            {
//                return $"{prettyPlayer}入玉宣言";
//            }

//            if (!pos.IsLegal(m))
//            {
//                throw new ArgumentException($"合法手ではありません：{m.Usi()}、対象の局面：{pos.Pretty()}");
//            }

//            var to = m.To();
//            var prettySq = isRecapture ? "同" : Square.PrettySquare(to);
//            var p = m.IsDrop()
//                ? m.Dropped()
//                : pos.PieceAt(m.From()).Colorless();
//            // to に移動できる駒
//            var candidates = pos
//                .EnumerateAttackers(pos.Player, p, to);

//            if (m.IsDrop())
//            {
//                // 駒移動の可能性もあるなら、"打" をつける
//                string exp = candidates.Popcount() > 0 ? "打" : "";
//                return $"{prettyPlayer}{prettySq}{PrettyPiece(p)}{exp}";
//            }
//            else
//            {
//                Debug.Assert(candidates.Popcount() >= 1);

//                var from = m.From();
//                // 成れる可能性があるか。
//                var canPromote = pos.IsLegal(MoveExtensions.MakeMove(from, to, true));
//                // あるなら、不成をつける必要がでてくる
//                var promote = canPromote ? m.IsPromote() ? "成" : "不成" : "";
//                var result = $"{prettyPlayer}{prettySq}{PrettyPiece(p)}";

//                // 移動が１通りしかないとき
//                if (candidates.Popcount() == 1)
//                {
//                    return result + promote;
//                }
//                // ２通り以上 かつ 桂馬を動かすとき
//                else if (p == Piece.Knight)
//                {
//                    Debug.Assert(candidates.Popcount() == 2);

//                    var knightFiles = candidates
//                        .Select(sq => Square.FileOf(sq));
//                    var left = pos.Player == Color.Black
//                        ? knightFiles.First()
//                        : knightFiles.Skip(1).First();
//                    var movedFile = Square.FileOf(from);
//                    var relative = movedFile == left ? Relative.Hidari : Relative.Migi;

//                    return $"{result}{relative.Pretty()}{promote}";
//                }
//                else
//                {
//                    // https://www.shogi.or.jp/faq/kihuhyouki.html
//                    // 
//                    // 2つ以上可能性があるときは、
//                    // １．動作（上、寄、引）で１つに絞れる → 動作を追記
//                    // ２．左右（＋直）で１つに絞れる → 左右直を追記（馬、龍の場合は直は使わない）
//                    // ３．それ以外のとき、動作＋左右を追記する

//                    var md = DirectionExtensions
//                        .FromTo(from, to)
//                        .ToMoveDirection(pos.Player);
//                    var moveDirections = candidates
//                        .Select(from => DirectionExtensions
//                            .FromTo(from, to)
//                            .ToMoveDirection(pos.Player));

//                    // １．
//                    if (moveDirections.Where(x => x == md).Count() == 1)
//                    {
//                        return $"{result}{md.Pretty()}{promote}";
//                    }

//                    var r = DirectionExtensions
//                        .FromTo(m.From(), to)
//                        .ToRelative(pos.Player);
//                    var relatives = candidates
//                        .Select(from => DirectionExtensions
//                            .FromTo(from, to)
//                            .ToRelative(pos.Player));

//                    // ２．
//                    if (relatives.Where(x => x == r).Count() == 1)
//                    {
//                        if ((r == Relative.Sugu || r == Relative.SuguBack)
//                            && (p == Piece.ProBishop || p == Piece.ProRook))
//                        {
//                            // もう片方の Relative と反対のものを Sugu の代わりに使う
//                            r = relatives
//                                .Where(x => x != r)
//                                .First() == Relative.Hidari
//                                ? Relative.Migi : Relative.Hidari;
//                        }
//                        /*
//                         * １．で絞り込めず、２．で１つに絞り込めた場合、r=SuguBack はありえない
//                         * 理由：
//                         * 1. r に SuguBack が入る可能性のあるのは真後ろに下がれる 金、飛、馬、龍 のみ
//                         * 2. 金、飛 の場合は、引でかならず１つに絞り込めるので、ここに来ない
//                         * 3. 馬、龍の場合は上記分岐により、Sugu または SuguBack が Hidari or Migi に変換される
//                         */
//                        Debug.Assert(r != Relative.SuguBack);
//                        return $"{result}{r.Pretty()}{promote}";
//                    }

//                    // ３．
//                    return $"{result}{r.Pretty()}{md.Pretty()}{promote}";
//                }
//            }
//        }
//    }
//}
