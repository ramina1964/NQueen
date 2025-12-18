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
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForUniqueMode_RealSolver(int boardSize)
    {
        // Arrange: integration test with real solver to catch visualization regressions in Unique mode
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.Unique,
            displayMode: DisplayMode.Visualize,
            simulationResults: null,
            solutionFormatter: null,
            suppressUserDialogs: true);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert: same guards as All-mode test to catch renderer regressions
        mainVm.ObservableSolutions.Should().NotBeEmpty();
        mainVm.SelectedSolution.Should().NotBeNull();
        mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(boardSize);

        var solution = mainVm.SelectedSolution!;
        var positions = solution.Positions.ToList();
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

    [Theory]
    [InlineData(8)]
    public async Task FinalVisualization_ShouldShowValidFirstSolution_ForSingleMode_RealSolver(int boardSize)
    {
        // Arrange: integration test with real solver to catch visualization regressions in Single mode
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            simulationResults: null,
            solutionFormatter: null,
            suppressUserDialogs: true);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert: same guards as other modes
        mainVm.ObservableSolutions.Should().NotBeEmpty();
        mainVm.SelectedSolution.Should().NotBeNull();
        mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(boardSize);

        var solution = mainVm.SelectedSolution!;
        var positions = solution.Positions.ToList();
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
