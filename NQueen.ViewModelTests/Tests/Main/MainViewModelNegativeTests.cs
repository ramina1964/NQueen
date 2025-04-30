namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelNegativeTests
{
    [Fact]
    public async Task Solver_ShouldThrowExceptionForInvalidBoardSize()
    {
        // Arrange
        var solver = new Mock<ISolver>();
        solver.Setup(s => s.GetResultsAsync(-1, SolutionMode.Single, DisplayMode.Visualize))
              .ThrowsAsync(new ArgumentException("Invalid board size"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await solver.Object.GetResultsAsync(-1, SolutionMode.Single, DisplayMode.Visualize));
    }
}
