namespace NQueen.ViewModelTests.Tests.MainViewModelCase;

public class MainViewModelTests
{
    [Fact]
    public async Task Progress_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainViewModel.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainViewModel.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainViewModel.ProgressVisibility.Should().Be(Visibility.Hidden, _progressHiddenError);

        mainViewModel.ProgressValue.Should().BeGreaterThan(0, _progressValueUpdateError);

        mainViewModel.ProgressLabel.Should().NotBeNullOrEmpty(_progressLabelUpdateError);
    }

    [Fact]
    public async Task Chessboard_ShouldUpdateQueenPlacements()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainViewModel.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainViewModel.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainViewModel.Chessboard.Squares.Should().NotBeEmpty("chessboard squares should be populated");
        mainViewModel.Chessboard.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(8, "8 queens should be placed on the chessboard for an 8x8 board");
    }

    [Fact]
    public async Task Solutions_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainViewModel.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainViewModel.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainViewModel.ObservableSolutions.Should().NotBeEmpty("solutions should be populated during simulation");
        mainViewModel.SelectedSolution.Should().NotBeNull("a solution should be selected after simulation");
        mainViewModel.NoOfSolutions.Should().NotBe("0", "number of solutions should be updated");
    }

    [Fact]
    public void Cancel_ShouldStopSimulation()
    {
        // Arrange
        var mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            IsSimulating = true
        };

        // Act
        mainViewModel.CancelCommand.Execute(null);

        // Assert
        mainViewModel.IsSimulating.Should().BeFalse("simulation should stop when cancel is executed");
    }

    [Fact]
    public void Save_ShouldProcessSimulationResults()
    {
        // Arrange
        var mainViewModel = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
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

        mainViewModel.SimulationResults = results;

        // Act
        mainViewModel.SaveCommand.Execute(null);

        // Assert
        mainViewModel.IsIdle.Should().BeTrue("save command should not affect idle state");
    }

    private const string _progressHiddenError = "Progress bar should be hidden after simulation";
    private const string _progressValueUpdateError = "Progress value should update during simulation";
    private const string _progressLabelUpdateError = "Progress label should update during simulation";
}
