namespace NQueen.ViewModelTests.Setup;

public static class TestHelpers
{
    public static ServiceProvider CreateServiceProvider() =>
        TestServiceCollectionExtensions.InitializeForTests();

    public static MainViewModel CreateMainViewModel(
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null)
    {
        var serviceProvider = CreateServiceProvider();
        var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

        // Configure the MainViewModel instance through dependency injection
        mainViewModel.BoardSizeText = boardSize.ToString();
        mainViewModel.SolutionMode = solutionMode;
        mainViewModel.DisplayMode = displayMode;
        mainViewModel.SimulationResults = simulationResults ?? new SimulationResults([]);

        return mainViewModel;
    }

    public static MainViewModel CreateMainViewModelWithBoardSizeText(
        string boardSizeText,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        var mainVm = CreateMainViewModel();
        mainVm.SolutionMode = solutionMode;
        mainVm.BoardSizeText = boardSizeText;
        mainVm.DisplayMode = displayMode;

        return mainVm;
    }

    public static MainViewModel CreateMainViewModelWithBoardSize(
        int boardSize,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide) =>
            CreateMainViewModelWithBoardSizeText(
                boardSize.ToString(), solutionMode, displayMode);

    public static MainViewModel CreateMainViewModelWithSimulationResults(
        int boardSize,
        SolutionMode solutionMode,
        MockSaveFileDialogService saveFileDialogService)
    {
        var solver = new SimulationOrchestrator(new SolutionManager());
        var mainVm = new MainViewModel(solver, new TestDispatcher(), saveFileDialogService)
        {
            BoardSizeText = boardSize.ToString(),
            SolutionMode = solutionMode,
            IsIdle = true,
        };

        // Dynamically calculate simulation results
        var simulationResults = solver.GetResultsAsync(boardSize, solutionMode).Result;
        mainVm.SimulationResults = simulationResults;
        mainVm.NoOfSolutions = simulationResults.Solutions.Count().ToString();

        return mainVm;
    }

    public static Mock<ISolverWithToken> CreateMockSolver(IEnumerable<Solution> solutions)
    {
        var mockSolver = new Mock<ISolverWithToken>();
        mockSolver.Setup(
            s => s.GetResultsAsync(
                        It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
                  .ReturnsAsync(new SimulationResults(solutions));
        return mockSolver;
    }

    public static async Task WaitForSimulationCompletionAsync(MainViewModel mainVm)
    {
        var tcs = new TaskCompletionSource<bool>();
        mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;
        mainVm.SimulationCompleted -= (s, e) => tcs.SetResult(true);
    }

    public static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (condition() == false)
        {
            if (DateTime.UtcNow - start > timeout)
                throw new TimeoutException(ErrorMessages.GetTimeoutMessage(timeout));

            await Task.Delay(10);
        }
    }
}
