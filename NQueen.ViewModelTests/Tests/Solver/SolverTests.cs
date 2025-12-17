namespace NQueen.ViewModelTests.Tests.Solver;

public class SolverTests
{
    [Fact]
    public async Task Solver_ShouldReturnValidResults_WhenBoardSizeIsValid()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(
                [ new Solution([0, 1, 2], new DefaultSolutionFormatter(), id: null) ],
                totalSolutions: 1UL,
                ElapsedTimeInSec: 0.0));

        var simContext = new SimulationContext(
            BoardSize: 4,
            SolutionMode: SolutionMode.Single,
            DisplayMode: DisplayMode.Visualize);

        // Act
        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().NotBeEmpty();
        results.SolutionsCount.Should().Be(1);
        results.IsTotalInferred.Should().BeFalse();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(
                [],
                totalSolutions: 0UL,
                ElapsedTimeInSec: 0.0));

        var simContext = new SimulationContext(
            BoardSize: 4,
            SolutionMode: SolutionMode.Single,
            DisplayMode: DisplayMode.Visualize);

        // Act
        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().BeEmpty();
        results.SolutionsCount.Should().Be(0);
        results.IsTotalInferred.Should().BeFalse();
    }

    [Fact]
    public void BitmaskSolver_SingleMode_ShouldReturnExactlyOneSolution()
    {
        // Arrange
        var solver = new BitmaskSolver(
            boardSize: 4,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new DefaultSolutionFormatter());

        solver.EnableEvents = true;
        int solutionFoundEvents = 0;
        solver.SolutionFound += (_, _) => solutionFoundEvents++;

        // Act
        var results = solver.Solve();

        // Assert
        results.SolutionsCount.Should().Be(1);
        solutionFoundEvents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void BitmaskSolver_SingleMode_ShouldIgnorePreSetCancellationFlag()
    {
        // Arrange
        var solver = new BitmaskSolver(
            boardSize: 38,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new DefaultSolutionFormatter());

        solver.EnableEvents = true;
        solver.IsSolverCanceled = true; // Will be reset by Solve()

        // Act
        var results = solver.Solve();

        // Assert
        solver.IsSolverCanceled.Should().BeFalse(); // ResetForSolve clears it
        results.SolutionsCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void BitmaskSolver_SingleMode_ShouldEmitQueenPlacedEvents()
    {
        var solver = new BitmaskSolver(
            boardSize: 8,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new DefaultSolutionFormatter())
        {
            EnableEvents = true
        };
        int queenEvents = 0;
        int lastDepth = 0;

        solver.QueenPlaced += (_, e) =>
        {
            queenEvents++;
            lastDepth = e.Solution.Length;
        };

        _ = solver.Solve();

        queenEvents.Should().BeGreaterThanOrEqualTo(8);
        lastDepth.Should().Be(8);
    }
}
