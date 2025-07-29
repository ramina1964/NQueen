namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    public static ServiceProvider InitializeForTests()
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddTransient<IDispatcher, TestDispatcher>();
        services.AddTransient<ISaveFileDialogService, MockSaveFileDialogService>();

        // Register BoardState factory for dynamic board sizes
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver>(sp => sp.GetRequiredService<SolverEngine>());
        services.AddTransient<SimulationOrchestrator>();

        // Register shared ViewModels
        services.AddNQueenViewModels();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}