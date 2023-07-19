using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiLibSharp.Core.Tests;

[TestClass()]
public class UsiTests
{
    static IEnumerable<object[]> ParseMoveTestcases()
    {
        yield return new object[] { MoveExtensions.MakeMove(Square.S27, Square.S26), "2g2f" };
        yield return new object[] { MoveExtensions.MakeMove(Square.S88, Square.S22, true), "8h2b+" };
        yield return new object[] { MoveExtensions.MakeDrop(Piece.Pawn, Square.S55), "P*5e" };
        yield return new object[] { Move.Resign, "resign" };
        yield return new object[] { Move.Win, "win" };
    }

    [DataTestMethod]
    [DynamicData(nameof(ParseMoveTestcases), DynamicDataSourceType.Method)]
    public void ParseMoveTest(Move expected, string usi)
    {
        Usi.ParseMove(usi).Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("9j9a+")]
    [DataRow("+7776FU")]
    [DataRow("p*2a")]
    public void ParseMoveExceptionTest(string usi)
    {
        var act = () => Usi.ParseMove(usi);
        act.Should().Throw<FormatException>();
    }
}