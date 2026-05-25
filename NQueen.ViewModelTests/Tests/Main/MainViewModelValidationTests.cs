namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelValidationTests
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SingleDataModeHandling),
        MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldHandleAllCases_ForSingleMode(
        string? boardSizeText, bool isValid, string? expectedErrorKey)
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = boardSizeText!;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText))
            .Cast<string>()
            .ToList();

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
                errors.Should().Contain(expectedError);
            }
        }
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.ValidBoardSizes), MemberType = typeof(NQueenTestSets))]
    public void BoardSizeText_Validation_ShouldReportValidCases_WhenValid(int boardSize, SolutionMode solutionMode)
    {
        var mainVm = TestHelpers.CreateMainViewModelWithBoardSizeText(
            boardSize.ToString(), solutionMode);

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
    [MemberData(nameof(RespectSolutionModeLimitsData))]
    public void BoardSizeText_Validation_ShouldRespectSolutionModeLimits(
        string originalBoardSizeText,
        string finalBoardSizeText,
        SolutionMode originalSolutionMode,
        SolutionMode finalSolutionMode)
    {
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
        errors.Should().Contain(ErrorMessages.SizeTooLargeForAll); // 21 invalid for All (max 20)
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
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

    [Theory]
    [MemberData(nameof(ChessboardUpdatesWhenSwitchingToValidModeData))]
    public void Chessboard_Updates_WhenSwitchingToValidMode(string boardSizeText,
        SolutionMode invalidMode, SolutionMode validMode, int expectedBoardSize)
    {
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
    [MemberData(nameof(ChessboardDoesNotUpdateWhenSwitchingToInvalidModeData))]
    public void Chessboard_DoesNotUpdate_WhenSwitchingToInvalidMode(
        string boardSizeText, SolutionMode validMode, SolutionMode invalidMode)
    {
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

    // ---------------------- DATA PROVIDERS (dynamic) ----------------------

    public static IEnumerable<object[]> RespectSolutionModeLimitsData()
    {
        int invalidSize = Math.Max(BoardSettings.MaxSizeForUnique, BoardSettings.MaxSizeForAll) + 1; // exceeds both
        yield return new object[] { invalidSize.ToString(), invalidSize.ToString(), SolutionMode.Unique, SolutionMode.All };
    }

    public static IEnumerable<object[]> ChessboardUpdatesWhenSwitchingToValidModeData()
    {
        int invalidForUniqueAll = Math.Max(BoardSettings.MaxSizeForUnique, BoardSettings.MaxSizeForAll) + 1; // valid for Single
        // Ensure invalidForUniqueAll still within Single mode limit
        if (invalidForUniqueAll > BoardSettings.MaxSizeForSingle)
        {
            // Fallback: choose Single's max (still invalid for Unique/All if their max < Single's)
            invalidForUniqueAll = BoardSettings.MaxSizeForSingle;
        }
        // Case: size invalid for Unique/All, then becomes valid in Single
        yield return new object[] { invalidForUniqueAll.ToString(), SolutionMode.All, SolutionMode.Single, invalidForUniqueAll };
        yield return new object[] { invalidForUniqueAll.ToString(), SolutionMode.Unique, SolutionMode.Single, invalidForUniqueAll };

        // Also test the largest Single size if it is still invalid for Unique/All
        int largestSingle = BoardSettings.MaxSizeForSingle;
        if (largestSingle > Math.Max(BoardSettings.MaxSizeForUnique, BoardSettings.MaxSizeForAll))
        {
            yield return new object[] { largestSingle.ToString(), SolutionMode.All, SolutionMode.Single, largestSingle };
            yield return new object[] { largestSingle.ToString(), SolutionMode.Unique, SolutionMode.Single, largestSingle };
        }
    }

    public static IEnumerable<object[]> ChessboardDoesNotUpdateWhenSwitchingToInvalidModeData()
    {
        // Size valid for Single but invalid for Unique/All
        int invalidForUniqueAll = Math.Max(BoardSettings.MaxSizeForUnique, BoardSettings.MaxSizeForAll) + 1;
        if (invalidForUniqueAll > BoardSettings.MaxSizeForSingle)
        {
            // If new limits somehow surpass Single (unlikely), adjust to still test invalidation scenario
            invalidForUniqueAll = BoardSettings.MaxSizeForSingle; // will still be invalid for modes if their max < Single
        }

        // Existing large size (Single max) should be invalid for Unique/All
        int largestSingle = BoardSettings.MaxSizeForSingle;

        yield return new object[] { largestSingle.ToString(), SolutionMode.Single, SolutionMode.Unique };
        yield return new object[] { invalidForUniqueAll.ToString(), SolutionMode.Single, SolutionMode.All };
        yield return new object[] { invalidForUniqueAll.ToString(), SolutionMode.Single, SolutionMode.Unique };
    }
}
