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
        Console.WriteLine($"Testing BoardSizeText: '{boardSizeText}'");
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel(solutionFormatter: mockFormatter);
        mainVm.BoardSizeText = boardSizeText!;

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();
        
        Console.WriteLine($"Errors: {string.Join(", ", errors)}");

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
                Console.WriteLine($"Expected error: {expectedError}");
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
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModelWithBoardSizeText(
            boardSizeText, solutionMode);

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        // Assert
        errors.Should().BeEmpty();

        mainVm.HasErrors
            .Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.LargeValueCases),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldReportLargeValues_BySolutionMode(
        string boardSizeText, SolutionMode solutionMode, bool isValid, string? expectedErrorKey)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
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
    public void BoardSizeText_Validation_ShouldRespectSolutionModeLimits(
        string originalBoardSizeText,
        string finalBoardSizeText,
        SolutionMode originalSolutionMode,
        SolutionMode finalSolutionMode)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = originalSolutionMode;
        mainVm.BoardSizeText = originalBoardSizeText;

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        // Assert
        errors.Should().Contain(ErrorMessages.SizeTooLargeForUnique);
        mainVm.SolutionMode = finalSolutionMode;
        mainVm.BoardSizeText = finalBoardSizeText;
        errors = [.. mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>()];
        errors.Should().Contain(ErrorMessages.SizeTooLargeForAll);
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = "abc";

        // Act
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        // Assert
        errors.Should().NotBeEmpty();
        mainVm.BoardSizeText = "8";
        errors = [.. mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>()];
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }

    [Theory]
    [InlineData("18", SolutionMode.All, SolutionMode.Single, 18)]
    [InlineData("18", SolutionMode.Unique, SolutionMode.Single, 18)]
    [InlineData("21", SolutionMode.All, SolutionMode.Single, 21)]
    [InlineData("21", SolutionMode.Unique, SolutionMode.Single, 21)]
    public void Chessboard_Updates_WhenSwitchingToValidMode(string boardSizeText,
        SolutionMode invalidMode, SolutionMode validMode, int expectedBoardSize)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = invalidMode;
        mainVm.BoardSizeText = boardSizeText;

        // Act: Should be invalid, so chessboard should not update to expected size
        var initialSquaresCount = mainVm.ChessboardVm.Squares.Count;
        initialSquaresCount.Should().NotBe(expectedBoardSize * expectedBoardSize);

        // Now switch to a mode where the board size is valid
        mainVm.SolutionMode = validMode;

        // Assert: Chessboard should now be updated to expected size
        mainVm.ChessboardVm.Squares.Count.Should().Be(expectedBoardSize * expectedBoardSize);
        mainVm.BoardSize.Should().Be(expectedBoardSize);
        mainVm.HasErrors.Should().BeFalse();
    }

    [Theory]
    [InlineData("37", SolutionMode.Single, SolutionMode.Unique)]
    [InlineData("18", SolutionMode.Single, SolutionMode.All)]
    [InlineData("21", SolutionMode.Single, SolutionMode.All)]
    public void Chessboard_DoesNotUpdate_WhenSwitchingToInvalidMode(
        string boardSizeText, SolutionMode validMode, SolutionMode invalidMode)
    {
        // Arrange: Start with a valid mode and board size
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = validMode;
        mainVm.BoardSizeText = boardSizeText;

        // Act: Should be valid, so chessboard should be updated to expected size
        var expectedSize = ParsingUtils.ParseIntOrThrow(boardSizeText);
        var expectedSizeSquared = expectedSize * expectedSize;

        mainVm.ChessboardVm.Squares.Count.Should().Be(expectedSizeSquared);
        mainVm.HasErrors.Should().BeFalse();

        // Now switch to a mode where the board size is invalid
        mainVm.SolutionMode = invalidMode;

        // Assert: Errors should be present, but chessboard remains at last valid state
        mainVm.ChessboardVm.Squares.Count.Should().Be(expectedSizeSquared);
        mainVm.HasErrors.Should().BeTrue();
        mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().Should().NotBeEmpty();
    }
}
