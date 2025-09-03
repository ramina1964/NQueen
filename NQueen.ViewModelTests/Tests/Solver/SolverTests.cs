namespace NQueen.ViewModelTests.Tests.Solver;

// Todo: Add a test method for checking the simulation cancellation under execution.
public class SolverTests
{
    [Fact]
    public async Task Solver_ShouldReturnValidResults_WhenBoardSizeIsValid()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([new Solution(new int[] { 0, 1, 2 }, new DefaultSolutionFormatter(), null)], 0.0));

        // Act
        var results = await solver.Object.GetSimResultsAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(    
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([], 0.0));

        // Act
        var results = await solver.Object.GetSimResultsAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().BeEmpty();
    }
}
