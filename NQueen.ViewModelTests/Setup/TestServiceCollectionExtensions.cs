namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Initializes the DI container for tests, using the real SolverEngine.
    /// </summary>
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
        services.AddSingleton<ISolverBackEnd>(sp => (ISolverBackEnd)sp.GetRequiredService<ISolver>());

        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Initializes the DI container for tests, using a provided mock ISolver.
    /// </summary>
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

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}