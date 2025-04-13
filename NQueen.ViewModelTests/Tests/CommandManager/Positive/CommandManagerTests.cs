namespace NQueen.ViewModelTests.Tests.CommandManager.Positive;

public class CommandManagerTests
{
    [Theory]
    [InlineData("1", SolutionMode.Single, DisplayMode.Hide)]
    [InlineData("4", SolutionMode.Unique, DisplayMode.Visualize)]
    [InlineData("8", SolutionMode.Single, DisplayMode.Visualize)]
    [InlineData("12", SolutionMode.All, DisplayMode.Hide)]
    [InlineData("16", SolutionMode.Single, DisplayMode.Hide)]
    public async Task SimulateCommand_ShouldUpdateSimulationResults(
        string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = boardSizeText,
            SolutionMode = solutionMode,
            DisplayMode = displayMode
        };

        // Subscribe to the SimulationCompleted event
        _mainViewModel.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainViewModel.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        var noOfSolutions = _mainViewModel.SimulationResults.NoOfSolutions;
        _mainViewModel.BoardSizeText.Should().Be(boardSizeText);
        noOfSolutions.Should().BeGreaterThan(0, SolutionNumberError);
        _mainViewModel.IsSimulating.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_ShouldStopSimulation()
    {
        _mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            IsSimulating = true
        };

        // Arrange
        _mainViewModel.IsSimulating = true;

        // Act
        _mainViewModel.CancelCommand.Execute(null);

        // Assert
        Assert.False(_mainViewModel.IsSimulating);
    }

    [Fact]
    public void SaveCommand_ShouldProcessSimulationResults()
    {
        // Arrange
        _mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            IsIdle = true
        };

        var results = new SimulationResults(
            [
                new([0, 1, 2, 3], 1)
            ])
        {
            BoardSize = 8,
            NoOfSolutions = 1,
            ElapsedTimeInSec = 0.5
        };

        _mainViewModel.SimulationResults = results;

        // Act
        _mainViewModel.SaveCommand.Execute(null);

        // Assert
        Assert.True(_mainViewModel.IsIdle);
    }

    private MainViewModel _mainViewModel = null!;
    private const string SolutionNumberError =
        "The simulation should produce at least one solution";
}
