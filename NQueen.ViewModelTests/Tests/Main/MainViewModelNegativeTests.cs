namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelNegativeTests
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.InvalidInputs), MemberType = typeof(NQueenTestSets))]
    public async Task Solver_ShouldThrowExceptionForInvalidInputs(
        int boardSize, SolutionMode solutionMode)
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
