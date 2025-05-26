namespace NQueen.ViewModelTests.Helpers;

public static class AssertionHelpers
{
    public static void AssertChessboardState(MainViewModel mainVm, int expectedQueenCount)
    {
        mainVm.ChessboardVm.Squares.Should().NotBeEmpty(TestConst.ChessboardNotPopulatedError);
        mainVm.ChessboardVm.Squares.Count(sq => string.IsNullOrEmpty(sq.ImagePath) == false)
            .Should().Be(expectedQueenCount, TestConst.IncorrectQueenPlacementError);
    }

    public static void AssertSolutionsState(MainViewModel mainVm)
    {
        mainVm.ObservableSolutions.Should().NotBeEmpty(TestConst.NoOfSolsValueError);
        mainVm.SelectedSolution.Should().NotBeNull(TestConst.SolutionNotSelectedError);
        mainVm.NoOfSolutions.Should().NotBe("0", TestConst.SolutionNumberZeroError);
    }

    public static void AssertSavedContent(
        string? savedContent, int boardSize, SimulationResults simulationResults)
    {
        savedContent.Should().NotBeNullOrEmpty(TestConst.ContentNotSavedError);

        // Validate the presence of key information
        savedContent.Should().Contain(TestConst.BoardSizeLabel,
            TestConst.BoardSizeLabelError);

        savedContent.Should().Contain(TestConst.NoOfSolutionsLabel,
            TestConst.NoOfSolsLabelError);

        savedContent.Should().Contain(TestConst.ElapsedTimeLabel,
            TestConst.ElapsedTimeLabelError);

        // Validate the values
        savedContent.Should().Contain(boardSize.ToString(), TestConst.BoardSizeValueError);
        savedContent.Should().Contain(simulationResults.Solutions.Count().ToString(),
            TestConst.BoardSizeValueError);
    }
}
