using System.Text;

namespace ShogiLibSharp.Core;

public partial class Position
{
    public const string Hirate = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1";

    static readonly string[] DISP_RANK = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    static readonly string[] DISP_COLOR = { "先手", "後手" };
    static readonly string[] DISP_PIECE = {
        " ・", " 歩", " 香", " 桂", " 銀", " 金", " 角", " 飛", " 玉", " と", " 杏", " 圭", " 全", " 禁", " 馬", " 竜",
        "v・", "v歩", "v香", "v桂", "v銀", "v金", "v角", "v飛", "v玉", "vと", "v杏", "v圭", "v全", "v禁", "v馬", "v竜",
    };


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
            return GenerateMoves().Count == 0;
        }
    }

    // 千日手判定用
    class Board
    {
        readonly Piece[] _pieces = new Piece[81];
        readonly Hand[] _hands = new Hand[2];

        public Board(Position pos)
        {
            this._pieces = pos._pieces.ToArray();
            this._hands = pos._hands.ToArray();
        }

        public bool Equals(Board x)
        {
            return this._pieces.SequenceEqual(x._pieces) && this._hands.SequenceEqual(x._hands);
        }
    }

    /// <summary>
    /// 千日手の状態を取得する。
    /// </summary>
    public Repetition Repetition
    {
        get
        {
            //// 千日手でない状態は 100% 検出できる
            //if (CheckRepetitionWithHash() == Repetition.None)
            //{
            //    return Repetition.None;
            //}

            //// ハッシュ値の衝突による検出ミスでないことを一応チェックする

            var target = new Board(this);
            var sameCount = 0;
            var contCheck = new[] { true, true };
            var undoneMoves = new Stack<Move>();

            // 3回 current と同じ局面が現れるまで遡る（4回目=今の局面なので）
            while (this.Moves.Any())
            {
                contCheck[(int)Player.Inv()] &= this.InCheck;

                TryUndoMove(out var undoneMove);
                undoneMoves.Push(undoneMove);

                // 盤面と持ち駒が同じ
                if (target.Equals(new(this)))
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
                    return Repetition.Win; // -> これ、Winいらない（というか、ちゃんと判定できてない
                                           // ちゃんとやるなら、これ以前の局面すべてについて、千日手判定を行って、一番最初の連続王手の千日手を検出して。。。というふうにしないとおかしい）
                                           // それはじぶんでやってくれとなるので、手番側だけの連続王手をちぇっくすればいいはず
                }

                return Repetition.Draw;
            }
            else
                return Repetition.None;
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

                    Piece p = Usi.ParsePiece(board[i]);

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
                        var p = Usi.ParsePiece(c);

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
            var moves = string.Join(' ', this.Moves.Select(x => x.Move.ToUsi()));
            var temp = new Stack<Move>();

            while (TryUndoMove(out var m))
            {
                temp.Push(m);
            }

            var init = this.Sfen;

            foreach (var m in temp)
            {
                this.DoMoveUnsafe(m);
            }

            return $"sfen {init} moves {moves}";
        }
        set
        {
            const string PrefixSfen = "sfen ";
            const string PrefixMove = "moves ";

            if (!value.StartsWith(PrefixSfen))
                throw new ArgumentException("フォーマットが正しくありません。");

            this.Sfen = value[PrefixSfen.Length..];
            var moveStart = value.IndexOf(PrefixMove);

            if (moveStart == -1)
                return;

            var moves = value[(moveStart + PrefixMove.Length)..].Split();

            foreach (var m in moves)
            {
                this.DoMove(m.ToMove());
            }
        }
    }

    /// <summary>
    /// 指し手リスト。
    /// </summary>
    public List<(Move Move, Piece Piece)> Moves { get; } = new();

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
            return this[Squares.Index(r, f)];
        }
    }

    public Bitboard this[Color c]
    {
        get
        {
            return this._colorBB[(int)c];
        }
    }

    public Bitboard this[Piece p]
    {
        get
        {
            return this._pieceBB[(int)p];
        }
    }

    public Bitboard this[Color c, Piece p]
    {
        get
        {
            return this[p.Colored(c)];
        }
    }

    public Bitboard Occupancy
    {
        get
        {
            return this._colorBB[0] | this._colorBB[1];
        }
    }

    public Bitboard Checkers
    {
        get
        {
            return this._checkers;
        }
    }

    public Bitboard Pinned
    {
        get
        {
            return this._pinnedBy[(int)Player.Inv()];
        }
    }

    #endregion

    #region コンストラクタ

    public Position(string sfen)
    {
        this.Sfen = sfen;
    }

    public Position(Position pos)
    {
        this.Player = pos.Player;
        this.GamePly = pos.GamePly;
        this.Moves = pos.Moves.ToList();
        this._pieces = pos._pieces.ToArray();
        this._hands = pos._hands.ToArray();
        this._colorBB = pos._colorBB.ToArray();
        this._pieceBB = (Bitboard[])pos._pieceBB.Clone();
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
    /// 駒台を取得する
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Hand Hand(Color c)
    {
        return this._hands[(int)c];
    }

    public Bitboard SilverBB(Color c)
    {
        return this._silverBB[(int)c];
    }

    public Bitboard GoldBB(Color c)
    {
        return this._goldBB[(int)c];
    }

    public Bitboard BishopBB(Color c)
    {
        return this._bishopBB[(int)c];
    }

    public Bitboard RookBB(Color c)
    {
        return this._rookBB[(int)c];
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
            this._colorBB[(int)Player] ^= to;
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
    /// <exception cref="ArgumentException"></exception>
    public void DoMove(Move m)
    {
        if (!IsLegal(m))
        {
            throw new ArgumentException($"合法手ではありません。");
        }

        DoMoveUnsafe(m);
    }

    /// <summary>
    /// 局面を1手戻す。
    /// </summary>
    /// <param name="lastMove"></param>
    /// <returns></returns>
    public bool TryUndoMove(out Move lastMove)
    {
        if (this.Moves.Count == 0)
        {
            lastMove = Move.None;
            return false;
        }

        (lastMove, var captured) = this.Moves[^1];
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

        return true;
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
        if (m == Move.None)
            return false;

        var to = m.To();

        if (m.IsDrop())
        {
            // 置く場所が空きマスでない
            if (this[to] != Piece.Empty)
                return false;

            var k = m.Dropped();

            if (this.Hand(Player).Count(k) == 0)
                return false;

            switch (k)
            {
                case Piece.Pawn:
                    var droppable
                        = Bitboard.Rank(Player, Rank.R2, Rank.R9)
                        & Bitboard.PawnDropMask(this[Player, Piece.Pawn]);

                    // １段目以外もしくは二歩
                    if (!droppable.Test(to))
                        return false;

                    var ksq = this.King(Player.Inv());
                    var checkByPawn = Bitboard.PawnAttacks(Player.Inv(), ksq);

                    // 打ち歩詰め
                    if (checkByPawn.Test(to) && IsUchifuzume(to))
                        return false;
                    break;

                case Piece.Lance:
                    if (!Bitboard.Rank(Player, Rank.R2, Rank.R9).Test(to))
                        return false;
                    break;

                case Piece.Knight:
                    if (!Bitboard.Rank(Player, Rank.R3, Rank.R9).Test(to))
                        return false;
                    break;
            }
        }
        else
        {
            var from = m.From();
            var moved = this[from];
            var captured = this[to];

            if (moved == Piece.Empty)
                return false;

            if (moved.Color() != Player)
                return false;

            if (captured != Piece.Empty && captured.Color() == Player)
                return false;

            if (!Bitboard.Attacks(moved, from, Occupancy).Test(to))
                return false;

            var p = moved.Colorless();

            if (m.IsPromote())
            {
                // fromかtoが敵陣に入っている
                // そもそも成れる駒か
                if (!Squares.CanPromote(Player, from, to) || p.IsPromoted() || p == Piece.Gold)
                    return false;

                p = p.Promoted();
            }

            switch (p)
            {
                case Piece.Pawn:
                case Piece.Lance:
                    if (!Bitboard.Rank(Player, Rank.R2, Rank.R9).Test(to))
                        return false;
                    break;

                case Piece.Knight:
                    if (!Bitboard.Rank(Player, Rank.R3, Rank.R9).Test(to))
                        return false;
                    break;
            }
        }
        // 以下、回避手になっているかのチェック
        {
            var numCheckers = Checkers.Popcount();

            if (numCheckers == 0)
                return true;

            var ksq = this.King(Player);

            if (numCheckers == 1)
            {
                var csq = Checkers.LsbSquare();

                // checker との間に挟む
                if (m.IsDrop())
                {
                    return Bitboard.Between(ksq, csq).Test(to);
                }
                // 玉が逃げる
                else if (this[m.From()].Colorless() == Piece.King)
                {
                    return EnumerateAttackers(Player.Inv(), to, Occupancy ^ ksq).None();
                }
                // 駒移動で checker を取る or 間に入る
                else
                {
                    if (!(Bitboard.Between(ksq, csq) | Checkers).Test(to))
                        return false;

                    // ピンされている駒で王手は防げない
                    return !Pinned.Test(m.From());
                }
            }
            // 2枚以上の駒から王手されているとき
            else
            {
                // 玉自身が逃げるしかない
                if (m.IsDrop() || this[m.From()].Colorless() != Piece.King)
                    return false;

                return EnumerateAttackers(Player.Inv(), to, Occupancy ^ ksq).None();
            }
        }
    }

    /// <summary>
    /// 打ち歩詰めになるかどうか調べる。
    /// </summary>
    /// <param name="to">歩を打つと王手になるマス</param>
    /// <returns></returns>
    public bool IsUchifuzume(Square to)
    {
        var theirKsq = this.King(Player.Inv());
        var defenders = this.EnumerateAttackers(Player.Inv(), to) ^ theirKsq;

        if (defenders.Any())
        {
            var pinned = this.PinnedBy(Player);

            if (defenders.AndNot(pinned).Any())
            {
                return false;
            }

            // 現在ピンされていても、歩を打つことで
            // ピンが解除される位置なら防御可能
            defenders &= Bitboard.Line(theirKsq, to);

            if (defenders.Any())
            {
                return false;
            }
        }

        var occ = Occupancy ^ to;
        var evasionTo = Bitboard.KingAttacks(theirKsq).AndNot(this[Player.Inv()]);

        foreach (var kTo in evasionTo)
        {
            var attackers = EnumerateAttackers(Player, kTo, occ);

            if (attackers.None())
            {
                return false;
            }
        }

        return true;
    }

    ///// <summary>
    ///// 千日手のチェックを行う。
    ///// </summary>
    ///// <returns></returns>
    //public Repetition CheckRepetitionWithHash()
    //{
    //}

    /// <summary>
    /// sq に利きがある c の 種類 p の駒
    /// </summary>
    /// <param name="c"></param>
    /// <param name="p"></param>
    /// <param name="sq"></param>
    /// <returns></returns>
    public Bitboard EnumerateAttackers(Color c, Piece p, Square sq)
    {
        return this[c, p] & Bitboard.Attacks(c.Inv(), p, sq, Occupancy);
    }

    /// <summary>
    /// m が合法手か（連続王手の千日手のチェックはしない）
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public bool IsLegal(Move m)
    {
        return GenerateMoves().Contains(m);
    }

    /// <summary>
    /// 玉の位置を取得する。
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Square King(Color c)
    {
        return this[c, Piece.King].LsbSquare();
    }

    /// <summary>
    /// sq に利きのある c の駒をすべて列挙
    /// </summary>
    /// <param name="c"></param>
    /// <param name="sq"></param>
    /// <param name="occ"></param>
    /// <returns></returns>
    public Bitboard EnumerateAttackers(Color c, Square sq, Bitboard occ)
    {
        return (this[c, Piece.Pawn] & Bitboard.PawnAttacks(c.Inv(), sq))
             | (this[c, Piece.Lance] & Bitboard.LanceAttacks(c.Inv(), sq, occ))
             | (this[c, Piece.Knight] & Bitboard.KnightAttacks(c.Inv(), sq))
             | (this.SilverBB(c) & Bitboard.SilverAttacks(c.Inv(), sq))
             | (this.GoldBB(c) & Bitboard.GoldAttacks(c.Inv(), sq))
             | (this.BishopBB(c) & Bitboard.BishopAttacks(sq, occ))
             | (this.RookBB(c) & Bitboard.RookAttacks(sq, occ));
    }

    /// <summary>
    /// sq に利きのある c の駒をすべて列挙
    /// </summary>
    /// <param name="c"></param>
    /// <param name="sq"></param>
    /// <param name="occ"></param>
    /// <returns></returns>
    public Bitboard EnumerateAttackers(Color c, Square sq)
    {
        return EnumerateAttackers(c, sq, Occupancy);
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
        var bb = this[c] & Bitboard.Rank(c, Rank.R1, Rank.R3);
        if (bb.Popcount() < 10 + 1)
            return false;

        // (e)
        // 敵陣内の飛角馬竜
        var br = (this.BishopBB(c) | this.RookBB(c))
            & Bitboard.Rank(c, Rank.R1, Rank.R3);

        var point = br.Popcount() * 4
            + bb.Popcount() - 1
            + Hand(c).DeclarationPoint()
            + (int)c;

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

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"手番: {DISP_COLOR[(int)Player]}");
        sb.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １");
        sb.AppendLine("+---------------------------+");

        foreach (var rank in Ranks.All)
        {
            sb.Append('|');

            foreach (var file in Files.Reversed)
            {
                sb.Append(DISP_PIECE[(int)this[rank, file]]);
            }

            sb.Append('|');
            sb.AppendLine(DISP_RANK[(int)rank]);
        }

        sb.AppendLine("+---------------------------+");

        // 持ち駒
        foreach (var c in Colors.All)
        {
            sb.Append($"持ち駒（{DISP_COLOR[(int)c]}）:");

            if (Hand(c).None())
            {
                sb.AppendLine(" なし");
            }
            else
            {
                foreach (var p in Pieces.PawnToRook)
                {
                    if (Hand(c).Count(p) > 0)
                    {
                        sb.Append(DISP_PIECE[(int)p]);
                        sb.Append(Hand(c).Count(p));
                    }
                }

                sb.AppendLine();
            }
        }

        switch (Repetition)
        {
            case Repetition.None:
                sb.AppendLine("千日手: なし");
                break;
            case Repetition.Draw:
                sb.AppendLine("千日手: 引き分け");
                break;
            case Repetition.Lose:
            case Repetition.Win:
                sb.AppendLine("千日手: 連続王手の千日手");
                break;
        }

        sb.AppendLine($"ハッシュ値: {Hash}");
        sb.AppendLine($"SFEN: {Sfen}");

        return sb.ToString();
    }

    #endregion

    #region private メソッド

    /// <summary>
    /// `Player,` `_pieces`, `_hands` に合うように他の状態を設定
    /// </summary>
    void SetInternalStates()
    {
        for (int i = 0; i < this._colorBB.Length; ++i)
        {
            this._colorBB[i] = new();
        }

        for (int i = 0; i < this._pieceBB.Length; ++i)
        {
            this._pieceBB[i] = new();
        }

        foreach (var sq in Squares.All)
        {
            if (this[sq] != Piece.Empty)
            {
                this._colorBB[(int)this[sq].Color()] ^= sq;
                this._pieceBB[(int)this[sq]] ^= sq;
            }
        }

        GamePly = 1;
        Moves.Clear();

        SumUpBitboards(Color.Black);
        SumUpBitboards(Color.White);

        _checkers = ComputeCheckers();
        _pinnedBy[0] = ComputePinnedBy(Color.Black);
        _pinnedBy[1] = ComputePinnedBy(Color.White);
    }

    Bitboard ComputeCheckers()
    {
        return EnumerateAttackers(Player.Inv(), King(Player));
    }

    Bitboard ComputePinnedBy(Color c)
    {
        var theirKsq = King(c.Inv());
        var pinned = default(Bitboard);
        var pinnersCandidate = (this[c, Piece.Lance]
                & Bitboard.LancePseudoAttacks(c.Inv(), theirKsq))
            | (this.BishopBB(c) & Bitboard.BishopPseudoAttacks(theirKsq))
            | (this.RookBB(c) & Bitboard.RookPseudoAttacks(theirKsq));
        var occ = Occupancy;

        foreach (var sq in pinnersCandidate)
        {
            var between = Bitboard.Between(theirKsq, sq) & occ;
            if (between.Popcount() == 1) pinned |= between;
        }

        return pinned;
    }

    void SumUpBitboards(Color c)
    {
        _silverBB[(int)c] = this[c, Piece.Silver]
            | this[c, Piece.King]
            | this[c, Piece.ProBishop]
            | this[c, Piece.ProRook];

        _goldBB[(int)c] = this[c, Piece.Gold]
            | this[c, Piece.King]
            | this[c, Piece.ProPawn]
            | this[c, Piece.ProLance]
            | this[c, Piece.ProKnight]
            | this[c, Piece.ProSilver]
            | this[c, Piece.ProBishop]
            | this[c, Piece.ProRook];

        _bishopBB[(int)c] =
            this[c, Piece.Bishop] | this[c, Piece.ProBishop];

        _rookBB[(int)c] =
            this[c, Piece.Rook] | this[c, Piece.ProRook];
    }

    #endregion
}
