namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelNegativeTests
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.InvalidInputs), MemberType = typeof(NQueenTestSets))]
    public async Task Solver_ShouldThrowExceptionForInvalidInputs(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetResultsForBoardAsync(boardSize, solutionMode, DisplayMode.Visualize))
            .ThrowsAsync(new ArgumentException("Invalid input"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await solver.Object.GetResultsForBoardAsync(boardSize, solutionMode, DisplayMode.Visualize));
    }

    [Fact]
    public void MainViewModel_ShouldThrowException_WhenSolverIsNull()
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MainViewModel(null!, new TestDispatcher(),
            new MockSaveFileDialogService(), mockFormatter));
    }
}
