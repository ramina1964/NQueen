using NQueen.KernelBitmask.Services;

namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    public static ServiceProvider InitializeForTests()
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddSingleton<IDispatcher, TestDispatcher>();
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // Register BoardState factory for dynamic board sizes
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<SolverEngine>();

        // Register BitmaskSolverExtended as ISolverPruning
        services.AddBitmaskSolverServices(disableCap: true);

        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();
        
        // Build and return the service provider
        return services.BuildServiceProvider();
    }

    public static ServiceProvider InitializeForTestsWithMock(ISolverPruning mockSolver)
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddSingleton<IDispatcher, TestDispatcher>();
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // Register BoardState factory for dynamic board sizes
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();

        // Register the mock as ISolverPruning
        services.AddSingleton<ISolverPruning>(mockSolver);

        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}