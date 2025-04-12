namespace NQueen.ViewModelTests.Tests.CommandManager.Positive;

public class CommandManagerTests
{
    public CommandManagerTests()
    {
        // Use the real BackTrackingSolver and initialize MainViewModel
        var solver = new BackTrackingSolver(new SolutionManager());
        _viewModel = new MainViewModel(solver);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Hide)]
    [InlineData(4, SolutionMode.Unique, DisplayMode.Visualize)]
    [InlineData(16, SolutionMode.All, DisplayMode.Hide)]
    public async Task SimulateCommand_ShouldUpdateSimulationResults(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        _viewModel.BoardSize = boardSize;
        _viewModel.SolutionMode = solutionMode;
        _viewModel.DisplayMode = displayMode;

        // Act
        await Task.Run(() => _viewModel.SimulateCommand.Execute(null));

        // Assert
        _viewModel.BoardSize.Should().Be(boardSize);
        _viewModel.NoOfSolutions.Should().NotBeNullOrEmpty()
            .And.Match(noOfSolutions => int.Parse(noOfSolutions) > 0, SolutionNumberError);
        _viewModel.IsSimulating.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_ShouldStopSimulation()
    {
        // Arrange
        _viewModel.IsSimulating = true;

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsSimulating);
    }

    [Fact]
    public void SaveCommand_ShouldProcessSimulationResults()
    {
        // Arrange
        var results = new SimulationResults(
            [
                new([0, 1, 2, 3], 1)
            ])
        {
            BoardSize = 8,
            NoOfSolutions = 1,
            ElapsedTimeInSec = 0.5
        };

        _viewModel.SimulationResults = results;

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.True(_viewModel.IsIdle);
    }

    private readonly MainViewModel _viewModel;
    private const string SolutionNumberError =
        "The simulation should produce at least one solution";
}
