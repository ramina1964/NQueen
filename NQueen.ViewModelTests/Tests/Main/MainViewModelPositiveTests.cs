namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelPositiveTests : IDisposable
{
    public MainViewModelPositiveTests() =>
        _serviceProvider = TestHelpers.CreateServiceProvider();

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Chessboard_ShouldUpdateQueenPlacements(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        AssertionHelpers.AssertChessboardState(mainVm, boardSize);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Solutions_ShouldUpdateDuringSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        await TestHelpers.WaitForConditionAsync(() =>
            mainVm.ObservableSolutions.Any(), TimeSpan.FromSeconds(5));

        // Assert
        AssertionHelpers.AssertSolutionsState(mainVm);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public void Cancel_ShouldStopSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        mainVm.IsSimulating = true;

        // Act
        mainVm.CancelCommand.Execute(null);

        // Assert
        mainVm.IsSimulating.Should().BeFalse(TestConst.SimulationNotStoppedError);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Visualization_ShouldUpdateDuringSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        AssertionHelpers.AssertChessboardState(mainVm, boardSize);
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SmallValueCases), MemberType = typeof(NQueenTestSets))]
    public void Save_ShouldProcessSimulationResults(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var mockSaveFileDialogService = new MockSaveFileDialogService();
        var mainVm = TestHelpers.CreateMainViewModelWithSimulationResults(
            boardSize, solutionMode, mockSaveFileDialogService);

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
        mockSaveFileDialogService.WasCalled.Should().BeTrue(TestConst.SaveDialogNotShownError);

        AssertionHelpers.AssertSavedContent(
            mockSaveFileDialogService.SavedContent, boardSize, mainVm.SimulationResults);
    }

    [Fact]
    public async Task MainViewModel_ShouldUpdateSolutionsAfterSimulation()
    {
        // Arrange
        List<Solution> solutions = [new([1, 3, 0, 2])];
        var mockSolver = TestHelpers.CreateMockSolver(solutions);

        var mainViewModel = new MainViewModel(
            mockSolver.Object,
            new TestDispatcher(),
            new MockSaveFileDialogService());

        // Act
        await mainViewModel.SimulateCommand.ExecuteAsync(null);

        // Assert
        AssertionHelpers.AssertSolutionsState(mainViewModel);
    }

    public void Dispose() => _serviceProvider.Dispose();

    private readonly ServiceProvider _serviceProvider;
}
