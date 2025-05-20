namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelValidationTests
{
    [Theory]
    [InlineData("   ", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg))]
    [InlineData(null, false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg))]
    [InlineData("8", true, null)]
    [InlineData("0", false, nameof(ErrorMessages.SizeTooSmallMsg))]
    [InlineData("-1", false, nameof(ErrorMessages.SizeTooSmallMsg))]
    [InlineData("abc", false, nameof(ErrorMessages.InvalidIntegerError))]
    [InlineData("", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg))]
    [InlineData("1", true, null)]
    [InlineData("17", true, null)]
    [InlineData("18", true, null)]
    [InlineData("8.0", false, nameof(ErrorMessages.InvalidIntegerError))]
    [InlineData("4,5", false, nameof(ErrorMessages.InvalidIntegerError))]
    public void BoardSizeText_Validation_ShouldReportErrors(string? boardSizeText,
        bool isValid, string? expectedErrorKey)
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = boardSizeText!;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        if (isValid)
        {
            errors.Should().BeEmpty();
            mainVm.HasErrors.Should().BeFalse();
        }
        else
        {
            errors.Should().NotBeEmpty();
            mainVm.HasErrors.Should().BeTrue();

            // Check the error message if expectedErrorKey is provided
            if (expectedErrorKey != null)
            {
                var expectedError = expectedErrorKey switch
                {
                    nameof(ErrorMessages.SizeTooSmallMsg) => ErrorMessages.SizeTooSmallMsg,
                    nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) => ErrorMessages.ValueNullOrWhiteSpaceMsg,
                    nameof(ErrorMessages.InvalidIntegerError) => ErrorMessages.InvalidIntegerError,
                    _ => null
                };
                errors.Should().Contain(expectedError);
            }
        }
    }

    [Theory]
    [InlineData("1000", SolutionMode.Single, false, nameof(ErrorMessages.SizeTooLargeForSingle))]
    [InlineData("1000", SolutionMode.Unique, false, nameof(ErrorMessages.SizeTooLargeForUnique))]
    [InlineData("1000", SolutionMode.All, false, nameof(ErrorMessages.SizeTooLargeForAll))]
    [InlineData("18", SolutionMode.Unique, false, nameof(ErrorMessages.SizeTooLargeForUnique))]
    [InlineData("18", SolutionMode.All, false, nameof(ErrorMessages.SizeTooLargeForAll))]
    public void BoardSizeText_Validation_ShouldReportLargeValues_BySolutionMode(
        string boardSizeText, SolutionMode solutionMode, bool isValid, string? expectedErrorKey)
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = solutionMode;
        mainVm.BoardSizeText = boardSizeText;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        if (isValid)
        {
            errors.Should().BeEmpty();
            mainVm.HasErrors.Should().BeFalse();
        }
        else
        {
            errors.Should().NotBeEmpty();
            mainVm.HasErrors.Should().BeTrue();

            if (expectedErrorKey != null)
            {
                var expectedError = expectedErrorKey switch
                {
                    nameof(ErrorMessages.SizeTooLargeForSingle) => ErrorMessages.SizeTooLargeForSingle,
                    nameof(ErrorMessages.SizeTooLargeForUnique) => ErrorMessages.SizeTooLargeForUnique,
                    nameof(ErrorMessages.SizeTooLargeForAll) => ErrorMessages.SizeTooLargeForAll,
                    _ => null
                };
                errors.Should().Contain(expectedError);
            }
        }
    }

    [Theory]
    [InlineData("21", "18", SolutionMode.Unique, SolutionMode.All)]
    public void BoardSizeText_Validation_ShouldRespectSolutionModeLimits(string originalBoardSize,
        string finalBoardSize, SolutionMode originalSolutionMode, SolutionMode finalSolutionMode)
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = originalSolutionMode;
        mainVm.BoardSizeText = originalBoardSize;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().Contain(ErrorMessages.SizeTooLargeForUnique);

        mainVm.SolutionMode = finalSolutionMode;
        mainVm.BoardSizeText = finalBoardSize;
        errors = [.. mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>()];
        errors.Should().Contain(ErrorMessages.SizeTooLargeForAll);
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = "abc";
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().NotBeEmpty();

        mainVm.BoardSizeText = "8";
        errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }
}
