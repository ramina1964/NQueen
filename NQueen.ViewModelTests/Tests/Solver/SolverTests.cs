namespace NQueen.ViewModelTests.Tests.Solver;

// Todo: Add a test method for checking the simulation cancellation under execution.
public class SolverTests
{
    [Fact]
    public async Task Solver_ShouldReturnValidResults_WhenBoardSizeIsValid()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetResultsForBoardAsync(
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([new Solution([1, 3, 0, 2])], 0.0));

        // Act
        var results = await solver.Object.GetResultsForBoardAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetResultsForBoardAsync(    
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([], 0.0));

        // Act
        var results = await solver.Object.GetResultsForBoardAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().BeEmpty();
    }
}
