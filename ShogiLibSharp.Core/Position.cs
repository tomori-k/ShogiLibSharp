using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShogiLibSharp.Core
{
    public class Position
    {
        public static readonly string Hirate = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1";

        #region 内部状態

        private string initPos;
        private Board board = new Board();
        private Bitboard[] colorBB = new Bitboard[2];
        private Bitboard[,] pieceBB = new Bitboard[2, 16];
        // 指し手＋その指し手で確保した駒のペア
        private Stack<(Move Move, Piece Captured)> moves = new Stack<(Move, Piece)>();
        private Bitboard checkers;
        private Bitboard[] pinnedBy = new Bitboard[2];
        private Bitboard[] silvers = new Bitboard[2]; // 銀の動きができる駒　：銀、玉、馬、龍
        private Bitboard[] golds = new Bitboard[2];   // 金の動きができる駒　：金、玉、成駒
        private Bitboard[] bishops = new Bitboard[2]; // 角の動きができる駒　：角、馬
        private Bitboard[] rooks = new Bitboard[2];   // 飛車の動きができる駒：飛、龍

        #endregion

        #region 状態・プロパティ

        /// <summary>
        /// 手番
        /// </summary>
        public Color Player
        {
            get => board.Player;
            private set => board.Player = value;
        }

        /// <summary>
        /// Undo 可能か
        /// </summary>
        public bool IsUndoable
        {
            get { return moves.Count > 0; }
        }

        /// <summary>
        /// 手数
        /// </summary>
        public int GamePly { get; private set; } = 1;

        /// <summary>
        /// 最後の指し手の移動先にもとからあった駒
        /// 最後の指し手が駒打ち or 駒を取らない移動だった場合、EMPTY
        /// </summary>
        public Piece LastCaptured
        {
            get
            {
                if (moves.Count == 0)
                {
                    throw new InvalidOperationException("以前の指し手が存在しません");
                }
                return moves.Peek().Captured;
            }
        }

        #endregion

        #region コンストラクタ

        public Position ()
        {
            this.initPos = Sfen();
        }

        public Position(string sfen)
        {
            this.initPos = sfen;
            this.Set(sfen);
        }

        public Position(Board board)
        {
            this.board = board.Clone();
            SetInternalStates();
            this.initPos = Sfen();
        }

        #endregion

        #region public メソッド

        /// <summary>
        /// マス sq にある駒
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public Piece PieceAt(int sq)
        {
            return board.Squares[sq];
        }

        /// <summary>
        /// rank 段 file 筋 にある駒
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Piece PieceAt(int rank, int file)
        {
            return board.Squares[Square.Index(rank, file)];
        }

        /// <summary>
        /// c の駒台
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public ref CaptureList CaptureListOf(Color c)
        {
            return ref board.CaptureListOf(c);
        }

        /// <summary>
        /// c の駒のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public ref Bitboard ColorBB(Color c)
        {
            return ref colorBB[(int)c];
        }

        /// <summary>
        /// p の駒のビットボード
        /// </summary>
        /// <param name="p">先後情報を含む</param>
        /// <returns></returns>
        public ref Bitboard PieceBB(Piece p)
        {
            return ref pieceBB[(int)p.Color(), (int)p.Colorless()];
        }

        /// <summary>
        /// c の 種類 p の駒のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p">先後情報を含まない</param>
        /// <returns></returns>
        public ref Bitboard PieceBB(Color c, Piece p)
        {
            return ref PieceBB(p.Colored(c));
        }

        /// <summary>
        /// すべての駒のビットボード
        /// </summary>
        /// <returns></returns>
        public Bitboard GetOccupancy()
        {
            return colorBB[0] | colorBB[1];
        }

        /// <summary>
        /// 銀の動きができる駒（銀、玉、馬、龍）のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Bitboard Silvers(Color c)
        {
            return silvers[(int)c];
        }

        /// <summary>
        /// 金の動きができる駒（金、玉、成駒すべて）のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Bitboard Golds(Color c)
        {
            return golds[(int)c];
        }

        /// <summary>
        /// 角の動きができる駒（角、馬）のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Bitboard Bishops(Color c)
        {
            return bishops[(int)c];
        }

        /// <summary>
        /// 飛車の動きができる駒（飛、龍）のビットボード
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Bitboard Rooks(Color c)
        {
            return rooks[(int)c];
        }

        /// <summary>
        /// m で局面を進める。
        /// </summary>
        /// <param name="m">pseudo-legal な指し手。それ以外を渡したときの結果は不定。</param>
        public void DoMove_PseudoLegal(Move m)
        {
            var captured = PieceAt(m.To());

            // capture
            if (captured != Piece.Empty)
            {
                board.Squares[m.To()] = Piece.Empty;
                ColorBB(Player.Opponent()) ^= m.To();
                PieceBB(captured) ^= m.To();
                CaptureListOf(Player)
                    .Add(captured.Kind(), 1);
            }

            if (m.IsDrop())
            {
                var p = m.Dropped().Colored(Player);

                board.Squares[m.To()] = p;
                ColorBB(Player) ^= m.To();
                PieceBB(p) ^= m.To();
                CaptureListOf(Player)
                    .Add(p.Kind(), -1);
            }
            else
            {
                var before = PieceAt(m.From());
                var after = m.IsPromote()
                    ? before.Promoted()
                    : before;

                board.Squares[m.From()] = Piece.Empty;
                board.Squares[m.To()] = after;
                ColorBB(Player) ^= m.From();
                ColorBB(Player) ^= m.To();
                PieceBB(before) ^= m.From();
                PieceBB(after) ^= m.To();
            }

            if (captured != Piece.Empty)
            {
                SumUpBitboards(Color.Black);
                SumUpBitboards(Color.White);
            }
            else
            {
                SumUpBitboards(Player);
            }
            Player = Player.Opponent();
            GamePly += 1;
            moves.Push((m, captured));
            checkers = ComputeCheckers();
            pinnedBy[0] = ComputePinnedBy(Color.Black);
            pinnedBy[1] = ComputePinnedBy(Color.White);
        }

        /// <summary>
        /// m で局面を進める。
        /// </summary>
        /// <param name="m"></param>
        /// <exception cref="ArgumentException"></exception>
        public void DoMove(Move m)
        {
            if (!IsLegalMove(m))
            {
                throw new ArgumentException($"{m} は合法手ではありません、局面：{this}");
            }
            DoMove_PseudoLegal(m);
        }

        /// <summary>
        /// 局面を一手戻す
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void UndoMove()
        {
            if (!IsUndoable)
            {
                throw new InvalidOperationException("これ以上局面を遡ることはできません");
            }
            UndoMove_Impl();
        }

        /// <summary>
        /// sq に利きがある c の 種類 p の駒
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="sq"></param>
        /// <returns></returns>
        public Bitboard EnumerateAttackers(Color c, Piece p, int sq)
        {
            return PieceBB(c, p) & Bitboard.Attacks(c.Opponent(), p, sq, GetOccupancy());
        }

        /// <summary>
        /// Player の玉に王手がかかっているか
        /// </summary>
        /// <returns></returns>
        public bool InCheck()
        {
            return Checkers().Any();
        }

        /// <summary>
        /// 手番側の合法手の数が 0 か
        /// </summary>
        /// <returns></returns>
        public bool IsMated()
        {
            return Movegen.GenerateMoves(this).Count == 0;
        }

        /// <summary>
        /// m が合法手か（連続王手の千日手のチェックはしない）
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool IsLegalMove(Move m)
        {
            return Movegen.GenerateMoves(this).Contains(m);
        }

        /// <summary>
        /// c の玉の位置
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public int King(Color c)
        {
            return PieceBB(c, Piece.King).LsbSquare();
        }

        /// <summary>
        /// sq に利きのある c の駒をすべて列挙
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <param name="occ"></param>
        /// <returns></returns>
        public Bitboard EnumerateAttackers(Color c, int sq, Bitboard occ)
        {
            return (PieceBB(c, Piece.Pawn) & Bitboard.PawnAttacks(c.Opponent(), sq))
                 | (PieceBB(c, Piece.Lance) & Bitboard.LanceAttacks(c.Opponent(), sq, occ))
                 | (PieceBB(c, Piece.Knight) & Bitboard.KnightAttacks(c.Opponent(), sq))
                 | (Silvers(c) & Bitboard.SilverAttacks(c.Opponent(), sq))
                 | (Golds(c) & Bitboard.GoldAttacks(c.Opponent(), sq))
                 | (Bishops(c) & Bitboard.BishopAttacks(sq, occ))
                 | (Rooks(c) & Bitboard.RookAttacks(sq, occ));
        }

        /// <summary>
        /// sq に利きのある c の駒をすべて列挙
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sq"></param>
        /// <param name="occ"></param>
        /// <returns></returns>
        public Bitboard EnumerateAttackers(Color c, int sq)
        {
            return EnumerateAttackers(c, sq, GetOccupancy());
        }

        /// <summary>
        /// Player の玉に王手をかけている駒
        /// </summary>
        /// <returns></returns>
        public Bitboard Checkers()
        {
            return checkers;
        }

        /// <summary>
        /// c 側の駒によってピンされている駒
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Bitboard PinnedBy(Color c)
        {
            return pinnedBy[(int)c];
        }

        /// <summary>
        /// 千日手（同一局面が 4 回以上出現）をチェック
        /// </summary>
        /// <returns></returns>
        public Repetition CheckRepetition()
        {
            var current = board.Clone();
            var sameCount = 0;
            var contCheck = new[] { true, true };
            var undoneMoves = new Stack<Move>();

            // 3回 current と同じ局面が現れるまで遡る（4回目=今の局面なので）
            while (IsUndoable)
            {
                if (contCheck[(int)Player.Opponent()])
                {
                    contCheck[(int)Player.Opponent()] = InCheck();
                }

                undoneMoves.Push(moves.Peek().Move);
                UndoMove();

                // 手番と盤面と持ち駒が同じ
                if (board.Equals(current))
                    ++sameCount;

                if (sameCount >= 3)
                    break;
            }
            // もとに戻す
            foreach (var m in undoneMoves)
            {
                DoMove_PseudoLegal(m);
            }
            // ジャッジメント
            if (sameCount >= 3)
            {
                if (contCheck[(int)Player])
                {
                    return Repetition.Lose;
                }

                if (contCheck[(int)Player.Opponent()])
                {
                    return Repetition.Win;
                }

                return Repetition.Draw;
            }
            else
                return Repetition.None;
        }

        /// <summary>
        /// 手番側が宣言勝ちできるか
        /// </summary>
        /// <returns></returns>
        public bool CanDeclareWin()
        {
            // (b)
            var ksq = PieceBB(Piece.King.Colored(Player)).LsbSquare();
            if (Square.RankOf(Player, ksq) > 2)
                return false;

            // (c)
            if (InCheck())
                return false;

            // (d)
            // 敵陣内の駒（玉含む）
            var bb = ColorBB(Player) & Bitboard.Rank(Player, 0, 2);
            if (bb.Popcount() < 10 + 1)
                return false;

            // (e)
            // 敵陣内の飛角馬竜
            var br = (Bishops(Player) | Rooks(Player))
                & Bitboard.Rank(Player, 0, 2);

            var point = br.Popcount() * 4 + bb.Popcount() - 1 + (int)Player + CaptureListOf(Player).Point();

            return point >= 28;
        }

        /// <summary>
        /// sfen 文字列（例："lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1"） <br/>
        /// で盤面を設定
        /// </summary>
        /// <param name="sfen"></param>
        /// <exception cref="FormatException"></exception>
        public void Set(string sfen)
        {
            var splitted = sfen.Split(' ');

            if (splitted.Length < 4)
                throw new FormatException($"何らかの局面情報が抜け落ちています：{sfen}");

            var board = splitted[0];
            var player = splitted[1];
            var mochigoma = splitted[2];
            var gameply = splitted[3];

            this.board.Clear();

            // 盤面
            for (int i = 0, cnt = 0; i < board.Length; ++i)
            {
                if (cnt >= 81)
                    throw new FormatException($"余分な文字列が含まれています：{sfen}");

                if (board[i] == '/')
                    continue;

                var promoted = board[i] == '+';

                if (promoted && ++i >= board.Length)
                    throw new FormatException($"+の対象がありません：{sfen}");

                if (char.IsDigit(board[i]))
                {
                    cnt += board[i] - '0';
                }
                else
                {
                    var rank = cnt / 9;
                    var file = 8 - cnt % 9;
                    Piece p = Usi.FromUsi(board[i]);
                    if (promoted && p.Colorless() == Piece.King)
                        throw new FormatException($"玉は成れません：{sfen}");
                    this.board.Squares[Square.Index(rank, file)]
                        = promoted ? p.Promoted() : p;
                    cnt += 1;
                }
            }

            // 手番
            Player = player == "b" ? Color.Black
                   : player == "w" ? Color.White
                   : throw new FormatException($"手番がおかしいです：{sfen}");

            // 持ち駒
            if (mochigoma != "-")
            {
                var n = 0;
                foreach (var c in mochigoma)
                {
                    if (char.IsDigit(c))
                    {
                        n = n * 10 + c - '0';
                    }
                    else
                    {
                        Piece p = Usi.FromUsi(c);
                        if (p.Colorless() != Piece.King)
                        {
                            CaptureListOf(p.Color())
                                .Add(p.Colorless(), Math.Max(1, n));
                            n = 0;
                        }
                        else
                            throw new FormatException("駒台に玉があります：{sfen}");
                    }
                }
            }

            // 手数
            if (int.TryParse(gameply, out var ply))
            {
                GamePly = ply;
            }
            else
                throw new FormatException($"手数を変換できません：{sfen}");

            initPos = sfen;
            SetInternalStates();
        }

        /// <summary>
        /// 盤面を人が読みやすい文字列に変換
        /// </summary>
        /// <returns></returns>
        public string Pretty()
        {

            var sb = new StringBuilder();
            sb.AppendLine(board.Pretty());
            sb.AppendLine($"SFEN: {Sfen()}");
            switch (CheckRepetition())
            {
                case Repetition.Draw: sb.AppendLine("千日手"); break;
                case Repetition.Win: sb.AppendLine("連続王手の千日手（勝ち）"); break;
                case Repetition.Lose: sb.AppendLine("連続王手の千日手（負け）"); break;
                default: break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 現在の盤面を表す sfen 文字列を生成
        /// </summary>
        /// <returns></returns>
        public string Sfen()
        {
            var sb = new StringBuilder();
            for (int rank = 0; rank < 9; ++rank)
            {
                if (rank != 0) sb.Append('/');
                for (int file = 8; file >= 0; --file)
                {
                    int numEmpties = 0;
                    for (; file >= 0; --file)
                    {
                        if (PieceAt(rank, file) != Piece.Empty) break;
                        numEmpties += 1;
                    }
                    if (numEmpties > 0)
                    {
                        sb.Append(numEmpties);
                    }
                    if (file >= 0)
                    {
                        sb.Append(PieceAt(rank, file).Usi());
                    }
                }
            }

            // 手番
            sb.Append($" {Player.Usi()}");

            // 持ち駒
            if (CaptureListOf(Color.Black).Any() || CaptureListOf(Color.White).Any())
            {
                sb.Append(' ');
                foreach (Color c in Enum.GetValues(typeof(Color)))
                {
                    foreach (Piece p in PieceExtensions.PawnToRook.Reverse())
                    {
                        int n = CaptureListOf(c).Count(p);
                        var ps = p.Colored(c).Usi();
                        if (n == 1)
                            sb.Append(ps);
                        else if (n > 1)
                            sb.Append($"{n}{ps}");

                    }
                }
            }
            else
                sb.Append(" -");

            // 手数
            sb.Append($" {GamePly}");

            return sb.ToString();
        }

        public string SfenWithMoves()
        {
            return $"sfen {initPos} moves {string.Join(' ', moves.Select(x => x.Move.Usi()))}";
        }

        /// <summary>
        /// 現在の盤面
        /// </summary>
        /// <returns></returns>
        public Board ToBoard()
        {
            return board.Clone();
        }

        public override string ToString()
        {
            return Sfen();
        }

        #endregion

        #region private メソッド

        // Undo できるものと仮定
        private void UndoMove_Impl()
        {
            var (lastMove, captured) = moves.Pop();

            Player = Player.Opponent();
            GamePly -= 1;

            if (lastMove.IsDrop())
            {
                var dropped = PieceAt(lastMove.To());

                board.Squares[lastMove.To()] = Piece.Empty;
                ColorBB(Player) ^= lastMove.To();
                PieceBB(dropped) ^= lastMove.To();
                CaptureListOf(Player)
                    .Add(dropped.Kind(), 1);
            }
            else
            {
                var beforeUndo = PieceAt(lastMove.To());
                var afterUndo = lastMove.IsPromote()
                    ? beforeUndo.Demoted()
                    : beforeUndo;

                board.Squares[lastMove.To()] = captured;
                board.Squares[lastMove.From()] = afterUndo;
                ColorBB(Player) ^= lastMove.From();
                ColorBB(Player) ^= lastMove.To();
                PieceBB(beforeUndo) ^= lastMove.To();
                PieceBB(afterUndo) ^= lastMove.From();
                if (captured != Piece.Empty)
                {
                    ColorBB(Player.Opponent()) ^= lastMove.To();
                    PieceBB(captured) ^= lastMove.To();
                    CaptureListOf(Player)
                        .Add(captured.Kind(), -1);
                }
            }

            if (captured != Piece.Empty)
            {
                SumUpBitboards(Color.Black);
                SumUpBitboards(Color.White);
            }
            else
            {
                SumUpBitboards(Player);
            }
            checkers = ComputeCheckers();
            pinnedBy[0] = ComputePinnedBy(Color.Black);
            pinnedBy[1] = ComputePinnedBy(Color.White);
        }

        /// <summary>
        /// board に合うように他の状態を設定
        /// </summary>
        private void SetInternalStates()
        {
            board.Validate();

            colorBB = new Bitboard[2];
            pieceBB = new Bitboard[2, 16];

            for (int i = 0; i < 81; ++i)
            {
                if (board.Squares[i] != Piece.Empty)
                {
                    ColorBB(board.Squares[i].Color()) ^= i;
                    PieceBB(board.Squares[i]) ^= i;
                }
            }

            moves.Clear();
            SumUpBitboards(Color.Black);
            SumUpBitboards(Color.White);
            checkers = ComputeCheckers();
            pinnedBy[0] = ComputePinnedBy(Color.Black);
            pinnedBy[1] = ComputePinnedBy(Color.White);
        }

        private Bitboard ComputeCheckers()
        {
            return EnumerateAttackers(Player.Opponent(), King(Player));
        }

        private Bitboard ComputePinnedBy(Color c)
        {
            var theirKsq = King(c.Opponent());
            var pinned = default(Bitboard);
            var pinnersCandidate = (PieceBB(c, Piece.Lance)
                    & Bitboard.LancePseudoAttacks(c.Opponent(), theirKsq))
                | (Bishops(c) & Bitboard.BishopPseudoAttacks(theirKsq))
                | (Rooks(c) & Bitboard.RookPseudoAttacks(theirKsq));
            var occ = GetOccupancy();
            foreach (var sq in pinnersCandidate)
            {
                var between = Bitboard.Between(theirKsq, sq) & occ;
                if (between.Popcount() == 1) pinned |= between;
            }
            return pinned;
        }

        private void SumUpBitboards(Color c)
        {
            silvers[(int)c] = PieceBB(c, Piece.Silver)
                | PieceBB(c, Piece.King)
                | PieceBB(c, Piece.ProBishop)
                | PieceBB(c, Piece.ProRook);
            golds[(int)c] = PieceBB(c, Piece.Gold)
                | PieceBB(c, Piece.King)
                | PieceBB(c, Piece.ProPawn)
                | PieceBB(c, Piece.ProLance)
                | PieceBB(c, Piece.ProKnight)
                | PieceBB(c, Piece.ProSilver)
                | PieceBB(c, Piece.ProBishop)
                | PieceBB(c, Piece.ProRook);
            bishops[(int)c] =
                PieceBB(c, Piece.Bishop) | PieceBB(c, Piece.ProBishop);
            rooks[(int)c] =
                PieceBB(c, Piece.Rook) | PieceBB(c, Piece.ProRook);
        }

        #endregion
    }
}
