namespace NQueen.ViewModelTests.BackTrackingSolverTests.MainViewModelTests;

[Collection("Serial Test Collection")]
public class PositiveTests
{
    public PositiveTests()
    {
        var serviceProvider = TestHelpers.CreateServiceProvider();
        _dispatcher = serviceProvider.GetService<IDispatcher>() ?? new TestDispatcher();

        _mainVm = new MainViewModel(
            new BackTrackingSolver(new SolutionManager()),
            _dispatcher,
            new MockSaveFileDialogService())
        {
            BoardSizeText = "8",
            SolutionMode = SolutionMode.Single,
            DisplayMode = DisplayMode.Visualize
        };
    }

    // Todo: Find out why this method hangs on await tcs.Task.
    //[InlineData("4", SolutionMode.Single, DisplayMode.Visualize)]
    //[Theory]
    //[InlineData("8", SolutionMode.Unique, DisplayMode.Visualize)]
    //public async Task Progress_ShouldUpdateDuringSimulation(
    //    string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    //{
    //    // Arrange
    //    var tcs = new TaskCompletionSource<bool>();

    //    _mainVm.BoardSizeText = boardSizeText;
    //    _mainVm.SolutionMode = solutionMode;
    //    _mainVm.DisplayMode = displayMode;

    //    _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

    //    // Act
    //    _mainVm.SimulateCommand.Execute(null);
    //    await tcs.Task;

    //    // Assert
    //    _mainVm.ProgressVisibility.Should().Be(Visibility.Visible, TestConst.ProgressHiddenError);
    //    _mainVm.IsSingleRunning.Should().BeTrue("Progress bar should be indeterminate in Single mode.");
    //    _mainVm.ProgressLabel.Should().NotBeNullOrEmpty(TestConst.ProgressLabelUpdateError);
    //}

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
        _mainVm.ChessboardVm.Squares.Should().NotBeEmpty(TestConst.ChessboardNotPopulatedError);
        _mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
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

    //[Theory]
    //[InlineData("4", SolutionMode.Unique, DisplayMode.Visualize)]
    //[InlineData("8", SolutionMode.Single, DisplayMode.Visualize)]
    //public void Save_ShouldProcessSimulationResults(
    //    string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    //{
    //    // Arrange
    //    var mockSaveFileDialogService = new MockSaveFileDialogService();
    //    var mainVm = new MainViewModel(
    //        new BackTrackingSolver(new SolutionManager()),
    //        new TestDispatcher(),
    //        mockSaveFileDialogService)
    //    {
    //        BoardSizeText = boardSizeText,
    //        SolutionMode = solutionMode,
    //        DisplayMode = displayMode,
    //        IsIdle = true,
    //        SimulationResults = new SimulationResults([new([1, 3, 0, 2], 1)])
    //    };

    //    // Act
    //    mainVm.SaveCommand.Execute(null);

    //    // Assert
    //    mockSaveFileDialogService.WasCalled.Should().BeTrue("The save file dialog should be shown.");
    //    mockSaveFileDialogService.SavedContent.Should().NotBeNullOrEmpty("The content should be saved.");

    //    // Validate the presence of key information
    //    var savedContent = mockSaveFileDialogService.SavedContent!;
    //    savedContent.Should().Contain("Date & Time", "The date and time should be included in the saved content.");
    //    savedContent.Should().Contain("BoardSize", "The board size label should be included in the saved content.");
    //    savedContent.Should().Contain("No. of Solutions", "The solutions label should be included in the saved content.");
    //    savedContent.Should().Contain("Elapsed Time", "The elapsed time label should be included in the saved content.");

    //    // Validate the correctness of specific values
    //    savedContent.Should().Contain(boardSizeText, "The board size value should be correct.");
    //    savedContent.Should().Contain("1", "The number of solutions value should be correct.");
    //}

    private readonly IDispatcher _dispatcher;
    private readonly MainViewModel _mainVm;
}
