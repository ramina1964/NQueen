namespace NQueen.ViewModelTests.Tests.MainViewModelCase;

[Collection("Serial Test Collection")]
public class MainViewModelTests
{
    [Fact]
    public async Task Progress_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainVm.ProgressVisibility.Should().Be(Visibility.Hidden, TestConst.ProgressHiddenError);
        mainVm.ProgressValue.Should().BeGreaterThan(0, TestConst.ProgressValueUpdateError);
        mainVm.ProgressLabel.Should().NotBeNullOrEmpty(TestConst.ProgressLabelUpdateError);
    }

    [Fact]
    public async Task Chessboard_ShouldUpdateQueenPlacements()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainVm.Chessboard.Squares.Should().NotBeEmpty(TestConst.ChessboardNotPopulatedError);
        mainVm.Chessboard.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(8, TestConst.IncorrectQueenPlacementError);
    }

    [Fact]
    public async Task Solutions_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };

        mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainVm.ObservableSolutions.Should().NotBeEmpty();
        mainVm.SelectedSolution.Should().NotBeNull(TestConst.SolutionNotSelectedError);
        mainVm.NoOfSolutions.Should().NotBe("0", TestConst.SolutionNumberZeroError);
    }

    [Fact]
    public void Cancel_ShouldStopSimulation()
    {
        // Arrange
        var mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            IsSimulating = true
        };

        // Act
        mainVm.CancelCommand.Execute(null);

        // Assert
        mainVm.IsSimulating.Should().BeFalse(TestConst.SimulationNotStoppedError);
    }

    [Fact]
    public void Save_ShouldProcessSimulationResults()
    {
        // Arrange
        var mainVm = new MainViewModel(new BackTrackingSolver(new SolutionManager()))
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

        mainVm.SimulationResults = results;

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
        mainVm.IsIdle.Should().BeTrue(TestConst.SaveIdleStateError);
    }
}
