namespace NQueen.ViewModelTests.Tests.MainViewModelCase;

[Collection("Serial Test Collection")]
public class MainViewModelTests
{
    public MainViewModelTests()
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

    // Todo: Find out why this method hangs on await tcs.Task.
    //[InlineData("8", SolutionMode.Unique, DisplayMode.Visualize)]
    [Theory]
    [InlineData("4", SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Progress_ShouldUpdateDuringSimulation(
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
        _mainVm.ProgressVisibility.Should().Be(Visibility.Visible, TestConst.ProgressHiddenError);
        _mainVm.IsSingleRunning.Should().BeTrue("Progress bar should be indeterminate in Single mode.");
        _mainVm.ProgressLabel.Should().NotBeNullOrEmpty(TestConst.ProgressLabelUpdateError);
    }

    [Fact]
    public async Task Chessboard_ShouldUpdateQueenPlacements()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();

        _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        _mainVm.Chessboard.Squares.Should().NotBeEmpty(TestConst.ChessboardNotPopulatedError);
        _mainVm.Chessboard.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(8, TestConst.IncorrectQueenPlacementError);
    }

    [Fact]
    public async Task Solutions_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();

        _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        _mainVm.ObservableSolutions.Should().NotBeEmpty();
        _mainVm.SelectedSolution.Should().NotBeNull(TestConst.SolutionNotSelectedError);
        _mainVm.NoOfSolutions.Should().NotBe("0", TestConst.SolutionNumberZeroError);
    }

    [Fact]
    public void Cancel_ShouldStopSimulation()
    {
        // Arrange
        _mainVm.IsSimulating = true;

        // Act
        _mainVm.CancelCommand.Execute(null);

        // Assert
        _mainVm.IsSimulating.Should().BeFalse(TestConst.SimulationNotStoppedError);
    }

    [Theory]
    [InlineData("4", SolutionMode.Unique, DisplayMode.Visualize)]
    public async Task Save_ShouldProcessSimulationResults(
        string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();

        _mainVm.BoardSizeText = boardSizeText;
        _mainVm.SolutionMode = solutionMode;
        _mainVm.DisplayMode = displayMode;
        _mainVm.IsIdle = true;
        _mainVm.SimulationResults = new SimulationResults([new([1, 3, 0, 2], 1)]);

        _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainVm.SaveCommand.Execute(null);
        await tcs.Task;

        // Assert
        _mainVm.IsIdle.Should().BeTrue(TestConst.SaveIdleStateError);
    }

    private readonly IDispatcher _dispatcher;
    private readonly MainViewModel _mainVm;
}
