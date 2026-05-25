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
}
