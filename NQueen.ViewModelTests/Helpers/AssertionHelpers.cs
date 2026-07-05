namespace NQueen.ViewModelTests.Helpers;

public static class AssertionHelpers
{
    public static void AssertChessboardState(MainViewModel mainVm, int expectedQueenCount)
    {
        mainVm.ChessboardVm.Squares.ShouldNotBeEmpty(TestConst.ChessboardNotPopulatedError);
        mainVm.ChessboardVm.Squares.Count(sq => string.IsNullOrEmpty(sq.ImagePath) == false)
            .ShouldBe(expectedQueenCount, TestConst.IncorrectQueenPlacementError);
    }

    public static void AssertSolutionsState(MainViewModel mainVm)
    {
        mainVm.ObservableSolutions.ShouldNotBeEmpty(TestConst.NoOfSolsValueError);
        mainVm.SelectedSolution.ShouldNotBeNull(TestConst.SolutionNotSelectedError);
        mainVm.NoOfSolutions.ShouldNotBe("0", TestConst.SolutionNumberZeroError);
    }
}
