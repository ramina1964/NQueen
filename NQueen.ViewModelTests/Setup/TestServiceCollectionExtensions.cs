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

        // Register SolverEngine as both ISolver and ISolverBackEnd
        services.AddSingleton<ISolver, SolverEngine>();
        services.AddSingleton<ISolverBackEnd>(sp => sp.GetRequiredService<ISolver>());

        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();
        
        // Build and return the service provider
        return services.BuildServiceProvider();
    }

    public static ServiceProvider InitializeForTestsWithMock(ISolver mockSolver)
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddSingleton<IDispatcher, TestDispatcher>();
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // Register BoardState factory for dynamic board sizes
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();

        // Register the mock as both ISolver and ISolverBackEnd
        services.AddSingleton<ISolver>(mockSolver);
        services.AddSingleton<ISolverBackEnd>(mockSolver);

        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}