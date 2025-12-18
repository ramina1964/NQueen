namespace NQueen.ViewModelTests.Tests.Main;

public class VisualizationAllModeTests
{
    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForAllMode(int boardSize)
    {
        // Arrange: real solver via helper (not mocked) to exercise packing / materialization path
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.All,
            displayMode: DisplayMode.Visualize,
            simulationResults: null,
            solutionFormatter: null,
            suppressUserDialogs: true);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert basic state
        mainVm.ObservableSolutions.Should().NotBeEmpty("At least one solution should be materialized.");
        mainVm.SelectedSolution.Should().NotBeNull("First solution should be selected after visualization completes.");
        mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(boardSize, "All queens should be rendered on the board in final visualization.");

        var solution = mainVm.SelectedSolution!;
        var positions = solution.Positions.ToList();
        positions.Count.Should().Be(boardSize, "Solution should contain exactly N positions.");

        // Ensure more than one distinct row (regression guard: previously all queens appeared on first row)
        positions.Select(p => p.RowIndex).Distinct().Count().Should().BeGreaterThan(1, "Queens should not all occupy the same row.");

        // Validate N-Queen constraints (no row / diagonal conflicts)
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                var a = positions[i];
                var b = positions[j];
                a.RowIndex.Should().NotBe(b.RowIndex, "Two queens share a row.");
                Math.Abs(a.RowIndex - b.RowIndex).Should().NotBe(Math.Abs(a.ColumnIndex - b.ColumnIndex), "Two queens share a diagonal.");
            }
        }
    }

    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForUniqueMode(int boardSize)
    {
        // Arrange: use a mocked solver to avoid heavy unique enumeration while still exercising visualization
        var mockFormatter = new DefaultSolutionFormatter();
        // Known valid 8-queens solution
        var positionsArr = new int[] { 0, 4, 7, 5, 2, 6, 1, 3 };
        var solution = new Solution(positionsArr.Take(boardSize).ToArray(), mockFormatter, null);
        var mockSolver = NQueen.ViewModelTests.Setup.TestHelpers.CreateMockSolver(new List<Solution> { solution });

        var simContext = new SimulationContext(boardSize, SolutionMode.Unique, DisplayMode.Visualize);
        var mainVm = NQueen.ViewModelTests.Setup.TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object, simContext, simulationResults: null, mockFormatter);

        // Act
        await NQueen.ViewModelTests.Setup.TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        mainVm.ObservableSolutions.Should().NotBeEmpty();
        mainVm.SelectedSolution.Should().NotBeNull();
        var sol = mainVm.SelectedSolution!;
        var positions = sol.Positions.ToList();
        positions.Count.Should().Be(boardSize);
        positions.Select(p => p.RowIndex).Distinct().Count().Should().BeGreaterThan(1);
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                var a = positions[i];
                var b = positions[j];
                a.RowIndex.Should().NotBe(b.RowIndex);
                Math.Abs(a.RowIndex - b.RowIndex).Should().NotBe(Math.Abs(a.ColumnIndex - b.ColumnIndex));
            }
        }
    }
}
