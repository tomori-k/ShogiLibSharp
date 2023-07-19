namespace ShogiLibSharp.Core;

public enum Repetition
{
    /// <summary>
    /// 千日手でない
    /// </summary>
    None,

    /// <summary>
    /// 普通の千日手（連続王手の千日手でない）
    /// </summary>
    Draw,

    /// <summary>
    /// 連続王手の千日手、手番側の勝ち（非手番の反則負け）
    /// </summary>
    Win,

    /// <summary>
    /// 連続王手の千日手、手番側の負け（手番の反則負け）
    /// </summary>
    Lose,
}
