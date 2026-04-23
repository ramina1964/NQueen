using NQueen.Shared.Utils;

namespace NQueen.UnitTests.Tests.Shared;

public class ParsingUtilsTests
{
    // ── TryParseInt ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("0",    true,  0)]
    [InlineData("42",   true,  42)]
    [InlineData("-7",   true,  -7)]
    [InlineData("2147483647", true, int.MaxValue)]
    public void TryParseInt_ValidInput_ReturnsTrueAndCorrectValue(
        string input, bool expectedResult, int expectedValue)
    {
        var result = ParsingUtils.TryParseInt(input, out int value);
        result.Should().Be(expectedResult);
        value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("2147483648")]    // int.MaxValue + 1
    public void TryParseInt_InvalidInput_ReturnsFalseAndZero(string input)
    {
        var result = ParsingUtils.TryParseInt(input, out int value);
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    // ── ParseIntOrThrow ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("0",   0)]
    [InlineData("8",   8)]
    [InlineData("-1", -1)]
    public void ParseIntOrThrow_ValidInput_ReturnsCorrectValue(string input, int expected) =>
        ParsingUtils.ParseIntOrThrow(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("3.14")]
    public void ParseIntOrThrow_InvalidInput_ThrowsInvalidOperationException(string input) =>
        FluentActions.Invoking(() => ParsingUtils.ParseIntOrThrow(input))
            .Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{input}'*");
}
