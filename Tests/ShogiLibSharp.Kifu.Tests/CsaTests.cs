using FluentAssertions;
using ShogiLibSharp.Core;

namespace ShogiLibSharp.Kifu.Tests;

[TestClass]
public class CsaTests
{
    /*
     * 指し手に対するコメントの記録（それいがいの部分のコメントは読み飛ばす）
     * FormatException時の行番号
     * 非合法手が含まれているの例外、指し手についている手番の情報が誤りである場合の例外をそれぞれどうするか
     */
    [TestMethod]
    public void ParseTest()
    {
        var kifu = """
            '-------------"kifu.csa"--------------
            'バージョン
            V2.2
            '対局者名
            N+AlphaZero
            N-elmo
            '棋譜情報
            '棋戦名
            $EVENT:AlphaZero VS elmo
            '対局場所
            $SITE:Google
            '開始日時
            $START_TIME:2023/07/25 10:30:00
            '終了日時
            $END_TIME:2023/07/25 13:11:05
            '持ち時間（1時間25分+秒読み11秒）
            $TIME_LIMIT:01:25+11
            '戦型
            $OPENING:あいうえお
            '
            '初期局面
            P1-KY-KE-GI-KI-OU-KI-GI-KE-KY
            P2 * -HI *  *  *  *  * -KA * 
            P3-FU-FU-FU-FU-FU-FU-FU-FU-FU
            P4 *  *  *  *  *  *  *  *  * 
            P5 *  *  *  *  *  *  *  *  * 
            P6 *  *  *  *  *  *  *  *  * 
            P7+FU+FU+FU+FU+FU+FU+FU+FU+FU
            P8 * +KA *  *  *  *  * +HI * 
            P9+KY+KE+GI+KI+OU+KI+GI+KE+KY
            ' 先手番
            +
            '指し手と消費時間
            +2726FU
            T10
            'ただのコメント
            '*定跡1
            '*定跡2
            -8384FU
            '** 30 +2625FU -8485FU +9796FU
            '読み筋
            T2147483647
            +2625FU
            -8485FU
            +9796FU
            -4132KI
            +3938GI
            -9394FU
            +6978KI
            -7172GI
            +3736FU
            -3334FU
            +2524FU
            -2324FU
            +2824HI
            -8586FU
            +8786FU
            -8286HI
            +5968OU
            -8685HI
            +7776FU
            -2288UM
            +7988GI
            -0033KA
            +2421RY
            -3388UM
            +8977KE
            -8877UM
            +6877OU
            -8589RY
            +0024KE
            -0085KE
            +7766OU
            -0041GI
            +7868KI
            -8979RY
            +6858KI
            -0027FU
            +0083FU
            -2728TO
            +2432NK
            -3132GI
            +2128RY
            -0054KE
            +6656OU
            -7976RY
            +0066KI
            -5466KE
            +6766FU
            -8577NK
            +0022KA
            -7283GI
            +4746FU
            -8374GI
            +0045KE
            -6152KI
            +5647OU
            -7767NK
            +0072FU
            -5161OU
            +0082KA
            -6172OU
            +8291UM
            -4344FU
            +4533NK
            -4445FU
            +0082FU
            -6758NK
            +4958KI
            -4546FU
            +4737OU
            -7678RY
            +0068KE
            -7869RY
            +0059KY
            -0027FU
            +2827RY
            -0047KI
            +3847GI
            -4647TO
            +3747OU
            -8193KE
            +2725RY
            -0046FU
            +4746OU
            -0045FU
            +2545RY
            -0054GI
            +4515RY
            -0071KI
            +0084KI
            -6989RY
            +6876KE
            -1314FU
            +1525RY
            -0024FU
            +2534RY
            -7475GI
            +8281TO
            -7584GI
            +8171TO
            -7283OU
            +0075FU
            -8475GI
            +0065KI
            -0055KI
            +6555KI
            -5455GI
            +4637OU
            -0044FU
            +0065KI
            -7576GI
            +6575KI
            -0045KE
            +3445RY
            -4445FU
            +3332NK
            -5546GI
            +3728OU
            -0038KI
            +2838OU
            -0028HI
            +3828OU
            -4637NG
            +2818OU
            -3728NG
            +1828OU
            -9495FU
            +9182UM
            -8382OU
            +0081HI
            -8292OU
            +0091KI
            %TORYO
            '*投了
            T7
            '-------------------------------------

            """;

        var moveStrs = new[]
        {
            "+2726FU",
            "-8384FU",
            "+2625FU",
            "-8485FU",
            "+9796FU",
            "-4132KI",
            "+3938GI",
            "-9394FU",
            "+6978KI",
            "-7172GI",
            "+3736FU",
            "-3334FU",
            "+2524FU",
            "-2324FU",
            "+2824HI",
            "-8586FU",
            "+8786FU",
            "-8286HI",
            "+5968OU",
            "-8685HI",
            "+7776FU",
            "-2288UM",
            "+7988GI",
            "-0033KA",
            "+2421RY",
            "-3388UM",
            "+8977KE",
            "-8877UM",
            "+6877OU",
            "-8589RY",
            "+0024KE",
            "-0085KE",
            "+7766OU",
            "-0041GI",
            "+7868KI",
            "-8979RY",
            "+6858KI",
            "-0027FU",
            "+0083FU",
            "-2728TO",
            "+2432NK",
            "-3132GI",
            "+2128RY",
            "-0054KE",
            "+6656OU",
            "-7976RY",
            "+0066KI",
            "-5466KE",
            "+6766FU",
            "-8577NK",
            "+0022KA",
            "-7283GI",
            "+4746FU",
            "-8374GI",
            "+0045KE",
            "-6152KI",
            "+5647OU",
            "-7767NK",
            "+0072FU",
            "-5161OU",
            "+0082KA",
            "-6172OU",
            "+8291UM",
            "-4344FU",
            "+4533NK",
            "-4445FU",
            "+0082FU",
            "-6758NK",
            "+4958KI",
            "-4546FU",
            "+4737OU",
            "-7678RY",
            "+0068KE",
            "-7869RY",
            "+0059KY",
            "-0027FU",
            "+2827RY",
            "-0047KI",
            "+3847GI",
            "-4647TO",
            "+3747OU",
            "-8193KE",
            "+2725RY",
            "-0046FU",
            "+4746OU",
            "-0045FU",
            "+2545RY",
            "-0054GI",
            "+4515RY",
            "-0071KI",
            "+0084KI",
            "-6989RY",
            "+6876KE",
            "-1314FU",
            "+1525RY",
            "-0024FU",
            "+2534RY",
            "-7475GI",
            "+8281TO",
            "-7584GI",
            "+8171TO",
            "-7283OU",
            "+0075FU",
            "-8475GI",
            "+0065KI",
            "-0055KI",
            "+6555KI",
            "-5455GI",
            "+4637OU",
            "-0044FU",
            "+0065KI",
            "-7576GI",
            "+6575KI",
            "-0045KE",
            "+3445RY",
            "-4445FU",
            "+3332NK",
            "-5546GI",
            "+3728OU",
            "-0038KI",
            "+2838OU",
            "-0028HI",
            "+3828OU",
            "-4637NG",
            "+2818OU",
            "-3728NG",
            "+1828OU",
            "-9495FU",
            "+9182UM",
            "-8382OU",
            "+0081HI",
            "-8292OU",
            "+0091KI",
            "%TORYO",
        };

        using var sr = new StringReader(kifu);

        var parsed = Csa.Parse(sr);

        parsed.NameBlack.Should().Be("AlphaZero");
        parsed.NameWhite.Should().Be("elmo");
        parsed.Event.Should().Be("AlphaZero VS elmo");
        parsed.Site.Should().Be("Google");
        parsed.Opening.Should().Be("あいうえお");
        parsed.StartTime.Should().Be(new DateTimeOffset(2023, 7, 25, 10, 30, 0, TimeSpan.FromHours(9)));
        parsed.EndTime.Should().Be(new DateTimeOffset(2023, 7, 25, 13, 11, 5, TimeSpan.FromHours(9)));
        parsed.TimeLimit.Should().Be(new TimeSpan(1, 25, 0));
        parsed.Byoyomi.Should().Be(TimeSpan.FromSeconds(11));
        parsed.StartPos.Sfen.Should().Be(Position.Hirate);
        parsed.Moves
            .Select(x => x.MoveStr)
            .Should()
            .BeEquivalentTo(moveStrs, options => options.WithStrictOrdering());

        parsed.Moves[0].Should().Be(new CsaMove("+2726FU", MoveExtensions.MakeMove(Square.S27, Square.S26))
        {
            Comment = "'*定跡2",
            Elapsed = TimeSpan.FromSeconds(10),
        });

        parsed.Moves[1].Should().Be(new CsaMove("-8384FU", MoveExtensions.MakeMove(Square.S83, Square.S84))
        {
            Comment = "'** 30 +2625FU -8485FU +9796FU",
            Elapsed = TimeSpan.FromSeconds(2147483647),
        });

        parsed.Moves[^1].Should().Be(new CsaMove("%TORYO", Move.None)
        {
            Comment = "'*投了",
            Elapsed = TimeSpan.FromSeconds(7),
        });
    }
}