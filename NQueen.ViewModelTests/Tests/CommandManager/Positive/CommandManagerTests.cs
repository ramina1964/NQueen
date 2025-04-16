namespace NQueen.ViewModelTests.Tests.CommandManager.Positive;

[Collection("Serial Test Collection")]
public class CommandManagerTests
{
    public CommandManagerTests()
    {
        var serviceProvider = TestHelpers.CreateServiceProvider();
        _dispatcher = serviceProvider.GetService<IDispatcher>() ?? new TestDispatcher();

        _mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()), _dispatcher)
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };
    }

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

        _mainVm.BoardSizeText = boardSizeText;
        _mainVm.SolutionMode = solutionMode;
        _mainVm.DisplayMode = displayMode;

        _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        var noOfSolutions = _mainVm.SimulationResults.NoOfSolutions;
        _mainVm.BoardSizeText.Should().Be(boardSizeText);
        noOfSolutions.Should().BeGreaterThan(0, TestConst.SolutionNumberZeroError);
        _mainVm.IsSimulating.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_ShouldStopSimulation()
    {
        // Arrange
        _mainVm.IsSimulating = true;

        // Act
        _mainVm.CancelCommand.Execute(null);

        // Assert
        Assert.False(_mainVm.IsSimulating);
    }

    [Theory]
    [InlineData("4", SolutionMode.Single, DisplayMode.Hide)]
    public void SaveCommand_ShouldProcessSimulationResults(
        string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        _mainVm.BoardSizeText = boardSizeText;
        _mainVm.SolutionMode = solutionMode;
        _mainVm.DisplayMode = displayMode;

        _mainVm.SimulationResults = new SimulationResults([new([1, 3, 0, 2], 1)]);
        _mainVm.NoOfSolutions = "1";
        _mainVm.IsIdle = true;

        // Act
        _mainVm.SaveCommand.Execute(null);

        // Assert
        Assert.True(_mainVm.IsIdle);
    }

    private readonly MainViewModel _mainVm;
    private readonly IDispatcher _dispatcher;
}
