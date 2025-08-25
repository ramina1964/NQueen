namespace NQueen.ViewModelTests.Setup;

public static class TestHelpers
{
    public static ServiceProvider CreateServiceProvider() =>
        TestServiceCollectionExtensions.InitializeForTests();

    public static ServiceProvider CreateServiceProviderWithMock(ISolver mockSolver) =>
        TestServiceCollectionExtensions.InitializeForTestsWithMock(mockSolver);

    public static MainViewModel CreateMainViewModel(
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null,
        ISolutionFormatter? solutionFormatter = null)
    {
        var serviceProvider = CreateServiceProvider();
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();

        var mainViewModel = new MainViewModel(
            serviceProvider.GetRequiredService<ISolver>(),
            serviceProvider.GetRequiredService<IDispatcher>(),
            serviceProvider.GetRequiredService<ISaveFileDialogService>(),
            solutionFormatter);

        // Configure the MainViewModel instance
        mainViewModel.BoardSizeText = boardSize.ToString();
        mainViewModel.SolutionMode = solutionMode;
        mainViewModel.DisplayMode = displayMode;
        mainViewModel.SimulationResults = simulationResults ?? new SimulationResults([], 0);

        return mainViewModel;
    }

    public static MainViewModel CreateMainViewModelWithMock(
        ISolver mockSolver,
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null,
        ISolutionFormatter? solutionFormatter = null)
    {
        var serviceProvider = CreateServiceProviderWithMock(mockSolver);
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();

        var mainViewModel = new MainViewModel(
            mockSolver,
            serviceProvider.GetRequiredService<IDispatcher>(),
            serviceProvider.GetRequiredService<ISaveFileDialogService>(),
            solutionFormatter);

        // Configure the MainViewModel instance
        mainViewModel.BoardSizeText = boardSize.ToString();
        mainViewModel.SolutionMode = solutionMode;
        mainViewModel.DisplayMode = displayMode;
        mainViewModel.SimulationResults = simulationResults ?? new SimulationResults([], 0);

        return mainViewModel;
    }

    public static MainViewModel CreateMainViewModelWithBoardSizeText(
        string boardSizeText,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        ISolutionFormatter? solutionFormatter = null)
    {
        solutionFormatter ??= new TestSolutionFormatter();
        var mainVm = CreateMainViewModel(solutionFormatter: solutionFormatter);
        mainVm.SolutionMode = solutionMode;
        mainVm.BoardSizeText = boardSizeText;
        mainVm.DisplayMode = displayMode;

        return mainVm;
    }

    public static MainViewModel CreateMainViewModelWithBoardSize(
        int boardSize,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        ISolutionFormatter? solutionFormatter = null)
    {
        return CreateMainViewModelWithBoardSizeText(
            boardSize.ToString(), solutionMode, displayMode, solutionFormatter);
    }

    public static MainViewModel CreateMainViewModelWithSimulationResults(
        int boardSize,
        SolutionMode solutionMode,
        MockSaveFileDialogService saveFileDialogService,
        ISolutionFormatter? solutionFormatter = null)
    {
        var serviceProvider = CreateServiceProvider();
        var solver = serviceProvider.GetRequiredService<ISolver>();
        
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();
        var mainVm = new MainViewModel(
            solver,
            new TestDispatcher(),
            saveFileDialogService,
            solutionFormatter)
        {
            BoardSizeText = boardSize.ToString(),
            SolutionMode = solutionMode,
            IsIdle = true,
        };

        // Dynamically calculate simulation results
        var simulationResults = solver.GetResultsForBoardAsync(boardSize, solutionMode).Result;
        mainVm.SimulationResults = simulationResults;
        mainVm.NoOfSolutions = simulationResults.Solutions.Count().ToString();

        return mainVm;
    }

    public static Mock<ISolver> CreateMockSolver(IEnumerable<Solution> solutions)
    {
        var mockSolver = new Mock<ISolver>();
        mockSolver.Setup(
            s => s.GetResultsForBoardAsync(
                        It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
                  .ReturnsAsync(new SimulationResults(solutions, 0));
        
        return mockSolver;
    }

    public static async Task WaitForSimulationCompletionAsync(MainViewModel mainVm)
    {
        var tcs = new TaskCompletionSource<bool>();
        void handler(object? s, EventArgs e)
        {
            tcs.TrySetResult(true);
            mainVm.SimulationCompleted -= handler;
        }

        mainVm.SimulationCompleted += handler;

        try
        {
            mainVm.SimulateCommand.Execute(null);
            await tcs.Task;
        }
        catch (Exception ex)
        {
            // Log the exception and rethrow for test visibility
            Console.WriteLine($"Exception during simulation: {ex}");
            throw;
        }
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
