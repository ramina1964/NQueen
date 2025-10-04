using NQueen.Domain.Context;

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
        ISolutionFormatter? solutionFormatter = null,
        bool suppressUserDialogs = true)
    {
        var serviceProvider = CreateServiceProvider();
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();

        var vm = new MainViewModel(
            serviceProvider.GetRequiredService<ISolver>(),
            serviceProvider.GetRequiredService<IDispatcher>(),
            serviceProvider.GetRequiredService<ISaveFileDialogService>(),
            solutionFormatter);

        vm.SuppressUserDialogs = suppressUserDialogs;
        vm.SolutionMode = solutionMode;
        vm.DisplayMode = displayMode;
        vm.BoardSizeText = boardSize.ToString();
        vm.SimulationResults = simulationResults ?? new SimulationResults([], 0);
        return vm;
    }

    public static MainViewModel CreateMainViewModelWithMock(
        ISolver mockSolver,
        SimulationContext? simulationContext = null,
        SimulationResults? simulationResults = null,
        ISolutionFormatter? solutionFormatter = null,
        bool suppressUserDialogs = true)
    {
        var ctx = simulationContext ?? new SimulationContext(
            BoardSettings.DefaultBoardSize,
            SolutionMode.Single,
            DisplayMode.Hide);

        var serviceProvider = CreateServiceProviderWithMock(mockSolver);
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();

        var vm = new MainViewModel(
            mockSolver,
            serviceProvider.GetRequiredService<IDispatcher>(),
            serviceProvider.GetRequiredService<ISaveFileDialogService>(),
            solutionFormatter);

        vm.SuppressUserDialogs = suppressUserDialogs;
        vm.SolutionMode = ctx.SolutionMode;
        vm.DisplayMode = ctx.DisplayMode;
        vm.BoardSizeText = ctx.BoardSize.ToString();
        vm.SimulationResults = simulationResults ?? new SimulationResults([], 0);
        return vm;
    }

    public static MainViewModel CreateMainViewModelWithBoardSizeText(
        string boardSizeText,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        ISolutionFormatter? solutionFormatter = null,
        bool suppressUserDialogs = true)
    {
        var vm = CreateMainViewModel(
            boardSize: BoardSettings.DefaultBoardSize,
            solutionMode: solutionMode,
            displayMode: displayMode,
            solutionFormatter: solutionFormatter,
            suppressUserDialogs: suppressUserDialogs);

        vm.BoardSizeText = boardSizeText;
        return vm;
    }

    public static MainViewModel CreateMainViewModelWithBoardSize(
        int boardSize,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        ISolutionFormatter? solutionFormatter = null,
        bool suppressUserDialogs = true) =>
        CreateMainViewModel(
            boardSize: boardSize,
            solutionMode: solutionMode,
            displayMode: displayMode,
            solutionFormatter: solutionFormatter,
            suppressUserDialogs: suppressUserDialogs);

    public static async Task<MainViewModel> MainViewModelCreateMainViewModelWithSimulationResults(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode,
        MockSaveFileDialogService saveFileDialogService,
        ISolutionFormatter? solutionFormatter = null,
        bool suppressUserDialogs = true)
    {
        var serviceProvider = CreateServiceProvider();
        var solver = serviceProvider.GetRequiredService<ISolver>();
        solutionFormatter ??= serviceProvider.GetRequiredService<ISolutionFormatter>();

        var vm = new MainViewModel(
            solver,
            new TestDispatcher(),
            saveFileDialogService,
            solutionFormatter)
        {
            SuppressUserDialogs = suppressUserDialogs,
            SolutionMode = solutionMode,
            DisplayMode = displayMode,
            BoardSizeText = boardSize.ToString(),
            IsIdle = true
        };

        var simContext = new SimulationContext(boardSize, solutionMode, displayMode);
        var simulationResults = await solver.GetSimResultsAsync(simContext);

        vm.SimulationResults = simulationResults;
        vm.NoOfSolutions = simulationResults.Solutions.LongCount().ToString();
        return vm;
    }

    public static Mock<ISolver> CreateMockSolver(IEnumerable<Solution> solutions)
    {
        var mockSolver = new Mock<ISolver>();
        mockSolver
            .Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(solutions, 0));
        return mockSolver;
    }

    public static async Task WaitForSimulationCompletionAsync(MainViewModel mainVm)
    {
        var tcs = new TaskCompletionSource<bool>();
        void Handler(object? _, EventArgs __)
        {
            mainVm.SimulationCompleted -= Handler;
            tcs.TrySetResult(true);
        }

        mainVm.SimulationCompleted += Handler;
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;
    }

    public static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
                throw new TimeoutException($"Condition not met within {timeout.TotalMilliseconds}ms.");
            await Task.Delay(20);
        }
    }
}
