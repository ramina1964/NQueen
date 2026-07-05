using NQueen.Shared.Validation;

namespace NQueen.ViewModelTests.Tests.Validation;

/// <summary>
/// Coverage for BoardSizeValidator — the FluentValidation rules that gate the
/// board-size text box. Exercises the null/empty guard, the non-integer guard,
/// the below-minimum guard, and the per-mode upper bounds (Single=37, Unique=25,
/// All=25), plus the unsupported-mode ArgumentOutOfRangeException.
/// </summary>
[Trait("Category", "Validation")]
public class BoardSizeValidatorTests
{
    private static bool IsValid(SolutionMode mode, string input) =>
        new BoardSizeValidator(mode).Validate(input).IsValid;

    private static string? FirstError(SolutionMode mode, string input) =>
        new BoardSizeValidator(mode).Validate(input).Errors.FirstOrDefault()?.ErrorMessage;

    // ── Empty / whitespace ────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhiteSpace_IsInvalid(string input) =>
        IsValid(SolutionMode.All, input).ShouldBeFalse();

    [Fact]
    public void Validate_Empty_ReturnsNullOrWhiteSpaceMessage() =>
        FirstError(SolutionMode.All, "").ShouldBe(ErrorMessages.ValueNullOrWhiteSpaceMsg);

    [Fact]
    public void Validate_Null_ThrowsInvalidOperation()
    {
        // FluentValidation rejects a null root model before any rule runs, so callers
        // must never pass null. The GUI binds a non-null TextBox string, honouring this.
        var validator = new BoardSizeValidator(SolutionMode.All);
        Should.Throw<InvalidOperationException>(() => validator.Validate((string)null!));
    }

    // ── Non-integer ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("8x")]
    public void Validate_NonInteger_IsInvalid(string input) =>
        IsValid(SolutionMode.All, input).ShouldBeFalse();

    [Fact]
    public void Validate_NonInteger_ReturnsInvalidIntegerMessage() =>
        FirstError(SolutionMode.All, "abc").ShouldBe(ErrorMessages.InvalidIntegerError);

    // ── Below minimum ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Validate_BelowMinimum_IsInvalid(string input) =>
        IsValid(SolutionMode.All, input).ShouldBeFalse();

    [Fact]
    public void Validate_BelowMinimum_ReturnsOutOfRangeMessage() =>
        FirstError(SolutionMode.All, "0").ShouldBe(ErrorMessages.OutOfRangeMsg);

    // ── Valid values within range ─────────────────────────────────────────────

    [Theory]
    [InlineData(SolutionMode.Single, "1")]
    [InlineData(SolutionMode.Single, "37")]
    [InlineData(SolutionMode.Unique, "25")]
    [InlineData(SolutionMode.All, "8")]
    [InlineData(SolutionMode.All, "25")]
    public void Validate_WithinRange_IsValid(SolutionMode mode, string input) =>
        IsValid(mode, input).ShouldBeTrue();

    // ── Per-mode upper bounds ─────────────────────────────────────────────────

    [Fact]
    public void Validate_SingleMode_AboveMax_ReturnsSingleMessage() =>
        FirstError(SolutionMode.Single, (BoardSettings.MaxSizeForSingle + 1).ToString())
            .ShouldBe(ErrorMessages.OutOfRangeSingle);

    [Fact]
    public void Validate_UniqueMode_AboveMax_ReturnsUniqueMessage() =>
        FirstError(SolutionMode.Unique, (BoardSettings.MaxSizeForUnique + 1).ToString())
            .ShouldBe(ErrorMessages.OutOfRangeUnique);

    [Fact]
    public void Validate_AllMode_AboveMax_ReturnsAllMessage() =>
        FirstError(SolutionMode.All, (BoardSettings.MaxSizeForAll + 1).ToString())
            .ShouldBe(ErrorMessages.OutOfRangeAll);

    [Fact]
    public void Validate_SingleMode_AllowsLargerBoardThanAllMode()
    {
        // 30 is valid for Single (max 37) but invalid for All (max 25).
        IsValid(SolutionMode.Single, "30").ShouldBeTrue();
        IsValid(SolutionMode.All, "30").ShouldBeFalse();
    }

    // ── Unsupported mode ──────────────────────────────────────────────────────

    [Fact]
    public void Constructor_UnsupportedMode_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new BoardSizeValidator((SolutionMode)999));
}
