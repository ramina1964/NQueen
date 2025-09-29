namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelValidationTests
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SingleDataModeHandling),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldHandleAllCases_ForSingleMode(
        string? boardSizeText, bool isValid, string? expectedErrorKey)
    {
        Console.WriteLine($"Testing BoardSizeText: '{boardSizeText}'");
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel(solutionFormatter: mockFormatter);
        mainVm.BoardSizeText = boardSizeText!;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        Console.WriteLine($"Errors: {string.Join(", ", errors)}");

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
    [InlineData("1",  SolutionMode.Single)]
    [InlineData("37", SolutionMode.Single)]
    [InlineData("17", SolutionMode.Unique)]
    [InlineData("18", SolutionMode.Unique)]   // max valid Unique
    [InlineData("17", SolutionMode.All)]
    [InlineData("18", SolutionMode.All)]      // max valid All
    public void BoardSizeText_Validation_ShouldReportValidCases_WhenValid(
        string boardSizeText, SolutionMode solutionMode)
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModelWithBoardSizeText(
            boardSizeText, solutionMode);

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.LargeValueCases),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldReportLargeValues_BySolutionMode(
        string boardSizeText, SolutionMode solutionMode, bool isValid, string? expectedErrorKey)
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
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
    [InlineData("21", "19", SolutionMode.Unique, SolutionMode.All)]
    public void BoardSizeText_Validation_ShouldRespectSolutionModeLimits(
        string originalBoardSizeText,
        string finalBoardSizeText,
        SolutionMode originalSolutionMode,
        SolutionMode finalSolutionMode)
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.SolutionMode = originalSolutionMode;
        mainVm.BoardSizeText = originalBoardSizeText;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        errors.Should().Contain(ErrorMessages.SizeTooLargeForUnique);

        mainVm.SolutionMode = finalSolutionMode;
        mainVm.BoardSizeText = finalBoardSizeText;

        errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().Contain(ErrorMessages.SizeTooLargeForAll); // 19 invalid for All (max 18)
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = "abc";

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

        errors.Should().NotBeEmpty();
        mainVm.BoardSizeText = "8";
        errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }

    // Uses sizes invalid for starting mode (19/21 for Unique/All) then switches to valid Single.
    [Theory]
    [InlineData("19", SolutionMode.All,    SolutionMode.Single, 19)]
    [InlineData("19", SolutionMode.Unique, SolutionMode.Single, 19)]
    [InlineData("21", SolutionMode.All,    SolutionMode.Single, 21)]
    [InlineData("21", SolutionMode.Unique, SolutionMode.Single, 21)]
    public void Chessboard_Updates_WhenSwitchingToValidMode(string boardSizeText,
        SolutionMode invalidMode, SolutionMode validMode, int expectedBoardSize)
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var vm = TestHelpers.CreateMainViewModel();

        vm.SolutionMode = invalidMode;
        vm.BoardSizeText = boardSizeText;
        // Attempt to build while invalid (should NOT size to boardSizeText)
        BuildBoardIfValid(vm);

        int countWhileInvalid = vm.ChessboardVm.Squares.Count;

        vm.SolutionMode = validMode;
        vm.BoardSizeText = boardSizeText; // triggers validation in valid mode now
        BuildBoardIfValid(vm);

        vm.ChessboardVm.Squares.Count.Should().Be(expectedBoardSize * expectedBoardSize);
        countWhileInvalid.Should().NotBe(expectedBoardSize * expectedBoardSize);
        vm.HasErrors.Should().BeFalse();
    }

    [Theory]
    [InlineData("37", SolutionMode.Single, SolutionMode.Unique)] // 37 invalid for Unique
    [InlineData("19", SolutionMode.Single, SolutionMode.All)]    // 19 invalid for All
    [InlineData("19", SolutionMode.Single, SolutionMode.Unique)] // 19 invalid for Unique
    public void Chessboard_DoesNotUpdate_WhenSwitchingToInvalidMode(
        string boardSizeText, SolutionMode validMode, SolutionMode invalidMode)
    {
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var vm = TestHelpers.CreateMainViewModel();

        // Build valid board explicitly
        vm.SolutionMode = validMode;
        vm.BoardSizeText = boardSizeText;
        BuildBoardIfValid(vm);

        var expectedSize = ParsingUtils.ParseIntOrThrow(boardSizeText);
        var expectedCount = expectedSize * expectedSize;
        vm.ChessboardVm.Squares.Count.Should().Be(expectedCount);
        vm.HasErrors.Should().BeFalse();

        // Switch to invalid mode (size now exceeds limit of that mode)
        vm.SolutionMode = invalidMode;
        // Reassign same text (ensures validation runs under new mode)
        vm.BoardSizeText = boardSizeText;
        // Attempt to rebuild (should be rejected)
        BuildBoardIfValid(vm);

        vm.ChessboardVm.Squares.Count.Should().Be(expectedCount);
        vm.HasErrors.Should().BeTrue();
        vm.GetErrors(nameof(vm.BoardSizeText)).Cast<string>().Should().NotBeEmpty();
    }

    // Helper: only creates board if current (mode, size) pair is valid.
    private static void BuildBoardIfValid(MainViewModel vm)
    {
        if (!ParsingUtils.TryParseInt(vm.BoardSizeText, out var size))
            return;

        bool validForMode = vm.SolutionMode switch
        {
            SolutionMode.Single => size <= BoardSettings.MaxSizeForSingle,
            SolutionMode.Unique => size <= BoardSettings.MaxSizeForUnique,
            SolutionMode.All => size <= BoardSettings.MaxSizeForAll,
            _ => false
        } && size >= BoardSettings.MinSize;

        if (!validForMode)
            return;

        vm.ChessboardVm.WindowWidth = 800;
        vm.ChessboardVm.WindowHeight = 800;
        vm.ChessboardVm.CreateSquares(size);
    }
}
