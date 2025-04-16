namespace NQueen.ViewModelTests.Tests.CommandManager.Positive;

[Collection("Serial Test Collection")]
public class CommandManagerTests
{
    // Todo: Use CreateMainViewModel() to initialize _mainVm
    public CommandManagerTests()
    {
        var serviceProvider = TestHelpers.CreateServiceProvider();
        _dispatcher = serviceProvider.GetService<IDispatcher>() ?? new TestDispatcher();
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
        _mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()), _dispatcher)
        {
            BoardSizeText = boardSizeText,
            SolutionMode = solutionMode,
            DisplayMode = displayMode,
            Chessboard = new Chessboard(_dispatcher),
            SimulationResults = new SimulationResults([])
        };

        // Subscribe to the SimulationCompleted event
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
        _mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()), _dispatcher)
        {
            IsSimulating = true,
            SimulationResults = new SimulationResults([])
        };

        // Act
        _mainVm.CancelCommand.Execute(null);

        // Assert
        Assert.False(_mainVm.IsSimulating);
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

        _mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()), _dispatcher)
        {
            SimulationResults = results,
            IsIdle = true
        };

        // Act
        _mainVm.SaveCommand.Execute(null);

        // Assert
        Assert.True(_mainVm.IsIdle);
    }

    private MainViewModel _mainVm = null!;
    private readonly IDispatcher _dispatcher;
}
