using NQueen.Shared.Enums;

namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelNegativeTests
{
    [Theory]
    [InlineData(-1, SolutionMode.Single)]
    [InlineData(0, SolutionMode.Single)]
    [InlineData(38, SolutionMode.Single)]
    [InlineData(4.5, SolutionMode.Single)]
    [InlineData(-1, SolutionMode.Unique)]
    [InlineData(0, SolutionMode.Unique)]
    [InlineData(18, SolutionMode.Unique)]
    [InlineData(5.5, SolutionMode.Unique)]
    [InlineData(-1, SolutionMode.All)]
    [InlineData(0, SolutionMode.All)]
    [InlineData(18, SolutionMode.All)]
    [InlineData(6.5, SolutionMode.All)]
    [InlineData(8, (SolutionMode)999)]
    public async Task Solver_ShouldThrowExceptionForInvalidInputs(int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var solver = new Mock<ISolver>();
        solver.Setup(s => s.GetResultsAsync(boardSize, solutionMode, DisplayMode.Visualize))
              .ThrowsAsync(new ArgumentException("Invalid input"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await solver.Object.GetResultsAsync(boardSize, solutionMode, DisplayMode.Visualize));
    }

    [Fact]
    public void MainViewModel_ShouldThrowException_WhenSolverIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MainViewModel(null!, new TestDispatcher(), new MockSaveFileDialogService()));
    }
}
