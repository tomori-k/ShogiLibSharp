using System.Text;

namespace ShogiLibSharp.Core;

public class Position
{
    public const string Hirate = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1";

    #region 内部状態

    public readonly Piece[] _pieces = new Piece[81];
    public readonly Hand[] _hands = new Hand[2];
    public readonly Bitboard[] _colorBB = new Bitboard[2];
    public readonly Bitboard[] _pieceBB = new Bitboard[2 * 16];
    public readonly Bitboard[] _silverBB = new Bitboard[2];     // 銀の動きができる駒　：銀、玉、馬、龍
    public readonly Bitboard[] _goldBB = new Bitboard[2];       // 金の動きができる駒　：金、玉、成駒
    public readonly Bitboard[] _bishopBB = new Bitboard[2];     // 角の動きができる駒　：角、馬
    public readonly Bitboard[] _rookBB = new Bitboard[2];       // 飛車の動きができる駒：飛、龍

    Bitboard _checkers;
    readonly Bitboard[] _pinnedBy = new Bitboard[2];

    #endregion

    #region プロパティ

    /// <summary>
    /// 手番
    /// </summary>
    public Color Player
    {
        get;
        private set;
    }

    /// <summary>
    /// 手数
    /// </summary>
    public int GamePly
    {
        get;
        private set;
    }

    /// <summary>
    /// 手番側の玉に王手がかかっているかどうかを表す。
    /// </summary>
    public bool InCheck
    {
        get
        {
            return this._checkers.Any();
        }
    }

    /// <summary>
    /// 手番側の玉が詰んでいるかどうかを表す。
    /// </summary>
    public bool IsMated
    {
        get
        {
            return Movegen.GenerateMoves(this).Count == 0;
        }
    }

    /// <summary>
    /// 千日手の状態を取得する。
    /// </summary>
    public Repetition Repetition
    {
        get
        {

        }
    }

    /// <summary>
    /// 局面の SFEN 表現。
    /// </summary>
    public string Sfen
    {
        get
        {
            var sb = new StringBuilder();

            // todo: Ranks.All などを使った実装との速度比較 
            for (int rank = 0; rank < 9; ++rank)
            {
                if (rank != 0)
                    sb.Append('/');

                for (int file = 8; file >= 0; --file)
                {
                    int numEmpties = 0;

                    for (; file >= 0; --file)
                    {
                        if (this[(Rank)rank, (File)file] != Piece.Empty)
                            break;

                        numEmpties += 1;
                    }

                    if (numEmpties > 0)
                    {
                        sb.Append(numEmpties);
                    }
                    if (file >= 0)
                    {
                        sb.Append(this[(Rank)rank, (File)file].ToUsi());
                    }
                }
            }

            // 手番
            sb.Append($" {Player.ToUsi()}");

            // 持ち駒
            if (Hand(Color.Black).Any() || Hand(Color.White).Any())
            {
                sb.Append(' ');

                foreach (var c in Colors.All)
                {
                    foreach (var p in Pieces.RookToPawn)
                    {
                        int n = Hand(c).Count(p);

                        if (n == 0)
                            continue;

                        var s = p.Colored(c).ToUsi();

                        if (n == 1)
                        {
                            sb.Append(s);
                        }
                        else
                        {
                            sb.Append($"{n}{s}");
                        }
                    }
                }
            }
            else
            {
                sb.Append(" -");
            }

            // 手数
            sb.Append($" {GamePly}");

            return sb.ToString();
        }

        set
        {
            if (value.Split(' ') is not [var board, var player, var hand, var gameply, .. _])
            {
                throw new FormatException("不正な SFEN です。");
            }

            this._pieces.AsSpan().Fill(Piece.Empty);
            this._hands.AsSpan().Fill(Core.Hand.Zero);

            // 盤面
            for (int i = 0, cnt = 0; i < board.Length; ++i)
            {
                if (cnt >= 81)
                    throw new FormatException("マス目情報の個数が 81 より多いです。");

                if (board[i] == '/')
                    continue;

                var promoted = board[i] == '+';

                if (promoted && ++i >= board.Length)
                    throw new FormatException("成り駒の種類が指定されていません。");

                if (char.IsDigit(board[i]))
                {
                    cnt += board[i] - '0';
                }
                else
                {
                    var rank = (Rank)(cnt / 9);
                    var file = (File)(8 - cnt % 9);

                    Piece p = Usi.FromUsi(board[i]);

                    if (promoted && p.Colorless() == Piece.King)
                        throw new FormatException($"玉は成れません。");

                    this._pieces[(int)Squares.Index(rank, file)] = promoted ? p.Promoted() : p;
                    cnt += 1;
                }
            }

            // 手番
            Player = player == "b" ? Color.Black
                   : player == "w" ? Color.White
                   : throw new FormatException($"手番の形式が間違っています。");

            // 持ち駒
            if (hand != "-")
            {
                var n = 0;

                foreach (var c in hand)
                {
                    if (char.IsDigit(c))
                    {
                        n = n * 10 + c - '0';
                    }
                    else
                    {
                        var p = Usi.FromUsi(c);

                        if (p.Colorless() != Piece.King)
                        {
                            this._hands[(int)p.Color()].Add(p.Colorless(), Math.Max(1, n));
                            n = 0;
                        }
                        else
                        {
                            throw new FormatException("駒台に玉があります。");
                        }
                    }
                }
            }

            GamePly = int.Parse(gameply);

            SetInternalStates();
        }
    }

    /// <summary>
    /// SFEN 形式での、初期局面とそこからの指し手。
    /// </summary>
    public string SfenMoves
    {
        get
        {
            return $"sfen {initPos} moves {string.Join(' ', moves.Reverse().Select(x => x.Move.Usi()))}";
        }

        set
        {

        }
    }

    /// <summary>
    /// 指し手リスト。
    /// </summary>
    public List<(Move, Piece)> Moves { get; } = new();

    /// <summary>
    /// 局面のハッシュ値。
    /// </summary>
    public ulong Hash { get; private set; }

    /// <summary>
    /// 指定したマスにある駒を取得する。
    /// </summary>
    /// <param name="sq"></param>
    /// <returns></returns>
    public Piece this[Square sq]
    {
        get
        {
            return this._pieces[(int)sq];
        }
    }

    /// <summary>
    /// 指定したマスにある駒を取得する。
    /// </summary>
    /// <param name="r"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public Piece this[Rank r, File f]
    {
        get
        {
            return this[SquareExtensions.Index(r, f)];
        }
    }

    #endregion

    #region コンストラクタ

    public Position(Position pos)
    {
        this.Player = pos.Player;
        this._pieces = pos._pieces.ToArray();
        this._hands = pos._hands.ToArray();
        this._colorBB = pos._colorBB.ToArray();
        this._pieceBB = (Bitboard[])pos._pieceBB.Clone();
        //this.moves = new(pos.moves.Reverse()); // シャローコピーだけどまあいいか...
        this._checkers = pos._checkers;
        this._pinnedBy = pos._pinnedBy.ToArray();
        this._silverBB = (Bitboard[])pos._silverBB.Clone();
        this._goldBB = pos._goldBB.ToArray();
        this._bishopBB = (Bitboard[])pos._bishopBB.Clone();
        this._rookBB = pos._rookBB.ToArray();
    }

    #endregion

    #region public メソッド

    /// <summary>
    /// c の駒台
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Hand Hand(Color c)
    {
        return this._hands[(int)c];
    }

    /// <summary>
    /// 局面を進める。
    /// </summary>
    /// <param name="m">pseudo-legal な指し手。それ以外を渡したときの結果は不定。</param>
    public void DoMoveUnsafe(Move m)
    {
        var to = m.To();
        var captured = this[to];

        // capture
        if (captured != Piece.Empty)
        {
            this._pieces[(int)to] = Piece.Empty;
            this._colorBB[(int)Player.Inv()] ^= to;
            this._pieceBB[(int)captured] ^= to;
            this._hands[(int)Player].Add(captured.Kind(), 1);
        }

        if (m.IsDrop())
        {
            var p = m.Dropped().Colored(Player);

            this._pieces[(int)to] = p;
            this._colorBB((int)Player) ^= to;
            this._pieceBB[(int)p] ^= to;
            this._hands[(int)Player].Add(p.Kind(), -1);
        }
        else
        {
            var from = m.From();
            var before = this[from];
            var after = m.IsPromote()
                ? before.Promoted()
                : before;

            this._pieces[(int)from] = Piece.Empty;
            this._pieces[(int)to] = after;
            this._colorBB[(int)Player] ^= from;
            this._colorBB[(int)Player] ^= to;
            this._pieceBB[(int)before] ^= from;
            this._pieceBB[(int)after] ^= to;
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

        Player = Player.Inv();
        GamePly += 1;
        this.Moves.Add((m, captured));
        this._checkers = ComputeCheckers();
        this._pinnedBy[0] = ComputePinnedBy(Color.Black);
        this._pinnedBy[1] = ComputePinnedBy(Color.White);
    }

    /// <summary>
    /// 局面を進める。
    /// </summary>
    /// <param name="m"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void DoMove(Move m)
    {
        if (!IsLegal(m))
        {
            throw new InvalidOperationException($"{m.Usi()} は合法手ではありません、局面：{this}");
        }

        DoMoveUnsafe(m);
    }



    /// <summary>
    /// 局面を1手戻す。
    /// </summary>
    /// <param name="undoneMove"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool TryUndoMove(out Move undoneMove)
    {
        if (this.Moves.Count == 0)
        {
            throw new InvalidOperationException("これ以上局面を遡ることはできません。");
        }

        var (lastMove, captured) = this.Moves[^1];
        var to = lastMove.To();
        this.Moves.RemoveAt(this.Moves.Count - 1);

        Player = Player.Inv();
        GamePly -= 1;

        if (lastMove.IsDrop())
        {
            var dropped = this[to];

            this._pieces[(int)to] = Piece.Empty;
            this._colorBB[(int)Player] ^= to;
            this._pieceBB[(int)dropped] ^= to;
            this._hands[(int)Player].Add(dropped.Kind(), 1);
        }
        else
        {
            var from = lastMove.From();
            var beforeUndo = this[to];
            var afterUndo = lastMove.IsPromote()
                ? beforeUndo.Demoted()
                : beforeUndo;

            this._pieces[(int)to] = captured;
            this._pieces[(int)from] = afterUndo;
            this._colorBB[(int)Player] ^= from;
            this._colorBB[(int)Player] ^= to;
            this._pieceBB[(int)beforeUndo] ^= to;
            this._pieceBB[(int)afterUndo] ^= from;

            if (captured != Piece.Empty)
            {
                this._colorBB[(int)Player.Inv()] ^= to;
                this._pieceBB[(int)captured] ^= to;
                this._hands[(int)Player].Add(captured.Kind(), -1);
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

        _checkers = ComputeCheckers();
        _pinnedBy[0] = ComputePinnedBy(Color.Black);
        _pinnedBy[1] = ComputePinnedBy(Color.White);
    }

    /// <summary>
    /// 局面を1手戻す。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool TryUndoMove()
    {
        return this.TryUndoMove(out var _);
    }

    /// <summary>
    /// 疑似合法手かどうか判定する。
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public bool IsPseudoLegal(Move m)
    {

    }

    /// <summary>
    /// 千日手のチェックを行う。
    /// </summary>
    /// <returns></returns>
    public Repetition CheckRepetitionWithHash()
    {

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
        return PieceBB(c, p) & Bitboard.Attacks(c.Inv(), p, sq, GetOccupancy());
    }

    /// <summary>
    /// m が合法手か（連続王手の千日手のチェックはしない）
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public bool IsLegal(Move m)
    {
        return Movegen.GenerateMoves(this).Contains(m);
    }

    /// <summary>
    /// 玉の位置を取得する。
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Square King(Color c)
    {
        return this._pieceBB[(int)Piece.King.Colored(c)].LsbSquare();
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
        return (PieceBB(c, Piece.Pawn) & Bitboard.PawnAttacks(c.Inv(), sq))
             | (PieceBB(c, Piece.Lance) & Bitboard.LanceAttacks(c.Inv(), sq, occ))
             | (PieceBB(c, Piece.Knight) & Bitboard.KnightAttacks(c.Inv(), sq))
             | (Silvers(c) & Bitboard.SilverAttacks(c.Inv(), sq))
             | (Golds(c) & Bitboard.GoldAttacks(c.Inv(), sq))
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
        return _checkers;
    }

    /// <summary>
    /// c 側の駒によってピンされている駒
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Bitboard PinnedBy(Color c)
    {
        return _pinnedBy[(int)c];
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
            if (contCheck[(int)Player.Inv()])
            {
                contCheck[(int)Player.Inv()] = InCheck();
            }

            undoneMoves.Push(moves.Peek().Move);
            TryUndoMove();

            // 手番と盤面と持ち駒が同じ
            if (board.Equals(current))
                ++sameCount;

            if (sameCount >= 3)
                break;
        }
        // もとに戻す
        foreach (var m in undoneMoves)
        {
            DoMoveUnsafe(m);
        }
        // ジャッジメント
        if (sameCount >= 3)
        {
            if (contCheck[(int)Player])
            {
                return Repetition.Lose;
            }

            if (contCheck[(int)Player.Inv()])
            {
                return Repetition.Win;
            }

            return Repetition.Draw;
        }
        else
            return Repetition.None;
    }

    /// <summary>
    /// 宣言勝ちできるか判定する。
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool CanDeclareWin(Color c)
    {
        // (b)
        var ksq = King(c);

        if (ksq.Rank(c) > Rank.R3)
            return false;

        // (c)
        if (InCheck)
            return false;

        // (d)
        // 敵陣内の駒（玉含む）
        var bb = this._colorBB[(int)c] & Bitboard.Rank(c, 0, 2);
        if (bb.Popcount() < 10 + 1)
            return false;

        // (e)
        // 敵陣内の飛角馬竜
        var br = (this._bishopBB[(int)c] | this._rookBB[(int)c])
            & Bitboard.Rank(c, 0, 2);

        var point = br.Popcount() * 4 + bb.Popcount() - 1 + (int)c + Hand(c).DeclarationPoint();

        return point >= 28;
    }

    /// <summary>
    /// 宣言勝ちできるか判定する。
    /// </summary>
    /// <returns></returns>
    public bool CanDeclareWin()
    {
        return CanDeclareWin(Player);
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
    /// 現在の盤面
    /// </summary>
    /// <returns></returns>
    public Board ToBoard()
    {
        return board.Clone();
    }

    /// <summary>
    /// 現在の局面をコピーを作成
    /// </summary>
    /// <returns></returns>
    public Position Clone()
    {
        return new Position(this);
    }

    public override string ToString()
    {
        return Sfen();
    }

    #endregion

    #region private メソッド

    /// <summary>
    /// board に合うように他の状態を設定
    /// </summary>
    private void SetInternalStates()
    {
        board.Validate();

        _colorBB = new Bitboard[2];
        _pieceBB = new Bitboard[2 * 16];

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
        _checkers = ComputeCheckers();
        _pinnedBy[0] = ComputePinnedBy(Color.Black);
        _pinnedBy[1] = ComputePinnedBy(Color.White);
    }

    private Bitboard ComputeCheckers()
    {
        return EnumerateAttackers(Player.Inv(), King(Player));
    }

    private Bitboard ComputePinnedBy(Color c)
    {
        var theirKsq = King(c.Inv());
        var pinned = default(Bitboard);
        var pinnersCandidate = (PieceBB(c, Piece.Lance)
                & Bitboard.LancePseudoAttacks(c.Inv(), theirKsq))
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
        _silverBB[(int)c] = PieceBB(c, Piece.Silver)
            | PieceBB(c, Piece.King)
            | PieceBB(c, Piece.ProBishop)
            | PieceBB(c, Piece.ProRook);
        _goldBB[(int)c] = PieceBB(c, Piece.Gold)
            | PieceBB(c, Piece.King)
            | PieceBB(c, Piece.ProPawn)
            | PieceBB(c, Piece.ProLance)
            | PieceBB(c, Piece.ProKnight)
            | PieceBB(c, Piece.ProSilver)
            | PieceBB(c, Piece.ProBishop)
            | PieceBB(c, Piece.ProRook);
        _bishopBB[(int)c] =
            PieceBB(c, Piece.Bishop) | PieceBB(c, Piece.ProBishop);
        _rookBB[(int)c] =
            PieceBB(c, Piece.Rook) | PieceBB(c, Piece.ProRook);
    }

    #endregion
}
