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
        var simContext = new SimulationContext(
            BoardSize: boardSize,
            SolutionMode: solutionMode,
            DisplayMode: DisplayMode.Visualize);

        solver.Setup(s => s.GetSimResultsAsync(simContext))
            .ThrowsAsync(new ArgumentException("Invalid input"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await solver.Object.GetSimResultsAsync(simContext));
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
