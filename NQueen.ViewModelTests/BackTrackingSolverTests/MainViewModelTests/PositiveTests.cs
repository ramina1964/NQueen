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

    //[Fact]
    //public async Task Solutions_ShouldUpdateDuringSimulation()
    //{
    //    // Arrange
    //    var tcs = new TaskCompletionSource<bool>();

    //    _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

    //    // Act
    //    _mainVm.SimulateCommand.Execute(null);
    //    await tcs.Task;

    //    // Assert
    //    _mainVm.ObservableSolutions.Should().NotBeEmpty();
    //    _mainVm.SelectedSolution.Should().NotBeNull(TestConst.SolutionNotSelectedError);
    //    _mainVm.NoOfSolutions.Should().NotBe("0", TestConst.SolutionNumberZeroError);
    //}

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
    [InlineData("4", SolutionMode.Single)]
    [InlineData("4", SolutionMode.Unique)]
    [InlineData("8", SolutionMode.Single)]
    [InlineData("8", SolutionMode.Unique)]
    [InlineData("8", SolutionMode.All)]
    [InlineData("12", SolutionMode.Unique)]
    [InlineData("12", SolutionMode.All)]
    public async Task Save_ShouldProcessSimulationResults(
        string boardSizeText, SolutionMode solutionMode)
    {
        // Arrange
        var mockSaveFileDialogService = new MockSaveFileDialogService();
        var solver = new BackTrackingSolver(new SolutionManager());
        var mainVm = new MainViewModel(
            solver,
            new TestDispatcher(),
            mockSaveFileDialogService)
        {
            BoardSizeText = boardSizeText,
            SolutionMode = solutionMode,
            IsIdle = true,
        };

        // Dynamically calculate simulation results
        var boardSize = int.Parse(boardSizeText);
        var simulationResults = await solver.GetResultsAsync(boardSize, solutionMode);
        mainVm.SimulationResults = simulationResults;
        mainVm.NoOfSolutions = simulationResults.Solutions.Count().ToString();

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
        mockSaveFileDialogService.WasCalled.Should()
            .BeTrue(TestConst.SaveDialogNotShownError);

        mockSaveFileDialogService.SavedContent.Should()
            .NotBeNullOrEmpty(TestConst.ContentNotSavedError);

        // Validate the presence of key information
        var savedContent = mockSaveFileDialogService.SavedContent!;

        // Validate the labels
        savedContent.Should().Contain(TestConst.BoardSizeLabel,
            TestConst.BoardSizeLabelError);

        savedContent.Should().Contain(TestConst.NoOfSolutionsLabel,
            TestConst.NoOfSolutionsLabelError);

        savedContent.Should().Contain(TestConst.ElapsedTimeLabel,
            TestConst.ElapsedTimeLabelError);

        // Validate the values
        savedContent.Should().Contain(boardSizeText, TestConst.BoardSizeValueError);

        savedContent.Should().Contain(simulationResults.Solutions.Count().ToString(),
            TestConst.BoardSizeValueError);
    }

    [Fact]
    public async Task Visualization_ShouldUpdateDuringSimulation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        _mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        _mainVm.ChessboardVm.Squares.Should().NotBeEmpty(
            TestConst.ChessboardNotPopulatedDuringVisualizationError);

        _mainVm.ChessboardVm.Squares.Count(sq => !string.IsNullOrEmpty(sq.ImagePath))
            .Should().Be(8, "There should be 8 queens placed on the board.");
    }

    private readonly IDispatcher _dispatcher;
    private readonly MainViewModel _mainVm;
}
