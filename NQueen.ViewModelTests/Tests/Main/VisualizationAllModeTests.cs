namespace NQueen.ViewModelTests.Tests.Main;

public class VisualizationAllModeTests
{
    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForAllMode(int boardSize)
    {
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.All,
            displayMode: DisplayMode.Visualize,
            suppressUserDialogs: true);

        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        AssertValidVisualization(mainVm, boardSize);
    }

    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForUniqueMode_RealSolver(int boardSize)
    {
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.Unique,
            displayMode: DisplayMode.Visualize,
            suppressUserDialogs: true);

        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        AssertValidVisualization(mainVm, boardSize);
    }

    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForSingleMode_RealSolver(int boardSize)
    {
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            suppressUserDialogs: true);

        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        AssertValidVisualization(mainVm, boardSize);
    }

    // Asserts that the chessboard shows a valid N-Queen solution after visualization.
    private static void AssertValidVisualization(MainViewModel mainVm, int boardSize)
    {
        mainVm.ObservableSolutions.Should().NotBeEmpty("At least one solution should be materialized.");
        mainVm.SelectedSolution.Should().NotBeNull("First solution should be selected after visualization completes.");
        mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(boardSize, "All queens should be rendered on the board.");

        var positions = mainVm.SelectedSolution!.Positions.ToList();
        positions.Count.Should().Be(boardSize, "Solution should contain exactly N positions.");
        positions.Select(p => p.RowIndex).Distinct().Count()
            .Should().BeGreaterThan(1, "Queens should not all occupy the same row.");

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                var a = positions[i];
                var b = positions[j];
                a.RowIndex.Should().NotBe(b.RowIndex, "Two queens share a row.");
                Math.Abs(a.RowIndex - b.RowIndex)
                    .Should().NotBe(Math.Abs(a.ColumnIndex - b.ColumnIndex), "Two queens share a diagonal.");
            }
        }
    }
}

