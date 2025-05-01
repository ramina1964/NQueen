namespace NQueen.ViewModelTests.Tests.Solver;

public class SolverTests
{
    [Fact]
    public async Task Solver_ShouldReturnValidResults_WhenBoardSizeIsValid()
    {
        // Arrange
        var solver = new Mock<ISolver>();
        solver.Setup(s => s.GetResultsAsync(
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([new Solution([1, 3, 0, 2])]));

        // Act
        var results = await solver.Object.GetResultsAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolver>();
        solver.Setup(s => s.GetResultsAsync(
                It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .ReturnsAsync(new SimulationResults([]));

        // Act
        var results = await solver.Object.GetResultsAsync(
            4, SolutionMode.Single, DisplayMode.Visualize);

        // Assert
        results.Solutions.Should().BeEmpty();
    }
}
