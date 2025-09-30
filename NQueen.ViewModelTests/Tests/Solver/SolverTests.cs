namespace NQueen.ViewModelTests.Tests.Solver;

// Todo: Add a test method for checking the simulation cancellation under execution.
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
                totalSolutions: 1UL, ElapsedTimeInSec: 0.0));

        // Act
        var simContext = new SimulationContext(
            BoardSize: 4, SolutionMode.Single, DisplayMode: DisplayMode.Visualize);

        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults([], 0UL, 0.0));

        var simContext = new SimulationContext(
            BoardSize: 4,
            SolutionMode: SolutionMode.Single,
            DisplayMode: DisplayMode.Visualize);

        // Act
        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().BeEmpty();
    }
}
