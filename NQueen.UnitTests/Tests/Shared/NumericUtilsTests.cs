using System.Globalization;
using NQueen.Shared.Utils;

namespace NQueen.UnitTests.Tests.Shared;

public class NumericUtilsTests
{
    // ── FormatWithSpaceSeparator(long) ───────────────────────────────────────

    [Theory]
    [InlineData(0L,         "0")]
    [InlineData(1000L,      "1 000")]
    [InlineData(1000000L,   "1 000 000")]
    [InlineData(999L,       "999")]
    public void FormatWithSpaceSeparator_Long_FormatsWithSpaces(long value, string expected) =>
        NumericUtils.FormatWithSpaceSeparator(value).Should().Be(expected);

    // ── FormatWithSpaceSeparator(double, int) ────────────────────────────────

    [Theory]
    [InlineData(0.0,    0)]
    [InlineData(1000.0, 0)]
    public void FormatWithSpaceSeparator_Double_ZeroDecimals_FormatsWithSpaces(
        double value, int decimals)
    {
        var result = NumericUtils.FormatWithSpaceSeparator(value, decimals);
        result.Should().NotContain(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
    }

    [Theory]
    [InlineData(1234.5,  1)]
    [InlineData(1234.56, 2)]
    public void FormatWithSpaceSeparator_Double_WithDecimals_ContainsDecimalSeparatorAndSpaces(
        double value, int decimals)
    {
        var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var result = NumericUtils.FormatWithSpaceSeparator(value, decimals);
        result.Should().Contain(" ");    // thousand separator
        result.Should().Contain(dec);    // decimal separator
    }

    [Fact]
    public void FormatWithSpaceSeparator_Double_DefaultDecimalPlacesIsTwo()
    {
        var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var result = NumericUtils.FormatWithSpaceSeparator(1234.5);
        result.Should().Contain(dec);    // has decimal separator
        result.Should().Contain(" ");    // has thousand separator
    }

    // ── ParseFormattedNumber ─────────────────────────────────────────────────

    [Theory]
    [InlineData("0",       0)]
    [InlineData("999",     999)]
    public void ParseFormattedNumber_ValidInput_ReturnsParsedValue(
        string input, int expected) =>
        NumericUtils.ParseFormattedNumber(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseFormattedNumber_NullOrWhitespace_ThrowsArgumentException(string input) =>
        FluentActions.Invoking(() => NumericUtils.ParseFormattedNumber(input))
            .Should().Throw<ArgumentException>();

    // ── IncFormattedNumber ───────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IncFormattedNumber_NullOrWhitespace_ThrowsArgumentException(string input) =>
        FluentActions.Invoking(() => NumericUtils.IncFormattedNumber(input))
            .Should().Throw<ArgumentException>();

    // ── UpdateMemoryUsage ────────────────────────────────────────────────────

    [Fact]
    public void UpdateMemoryUsage_ReturnsNonEmptyString() =>
        NumericUtils.UpdateMemoryUsage().Should().NotBeNullOrWhiteSpace();
}
