namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelValidationTests
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SingleDataModeHandling),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldHandleAllCases_ForSingleMode(
        string? boardSizeText, bool isValid, string? expectedErrorKey)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = boardSizeText!;

        // Act
        var errors = mainVm
            .GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        // Assert
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
    [InlineData("1", SolutionMode.Single)]
    [InlineData("37", SolutionMode.Single)]
    [InlineData("17", SolutionMode.Unique)]
    [InlineData("17", SolutionMode.All)]
    public void BoardSizeText_Validation_ShouldReportValidCases_WhenValid(
        string boardSizeText, SolutionMode solutionMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = solutionMode;
        mainVm.BoardSizeText = boardSizeText;

        // Act
        var errors = mainVm
            .GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>().ToList();

        // Assert
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.LargeValueCases),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldReportLargeValues_BySolutionMode(
        string boardSizeText, SolutionMode solutionMode, bool isValid, string? expectedErrorKey)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = solutionMode;
        mainVm.BoardSizeText = boardSizeText;

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        // Assert
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
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = originalSolutionMode;
        mainVm.BoardSizeText = originalBoardSize;

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        // Assert
        errors.Should().Contain(ErrorMessages.SizeTooLargeForUnique);
        mainVm.SolutionMode = finalSolutionMode;
        mainVm.BoardSizeText = finalBoardSize;
        errors = [.. mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>()];
        errors.Should().Contain(ErrorMessages.SizeTooLargeForAll);
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = "abc";

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        // Assert
        errors.Should().NotBeEmpty();
        mainVm.BoardSizeText = "8";
        errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }
}
