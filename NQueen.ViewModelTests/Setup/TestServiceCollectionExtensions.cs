namespace NQueen.ViewModelTests.Setup;

/// <summary>
/// DI setup for ViewModel-focused tests (includes dispatcher, dialog service, and uncapped solver).
/// </summary>
public static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddViewModelTestServices(
        this IServiceCollection services,
        bool enableCap = false)
    {
        // Uncapped solver (test-friendly). Pass enableCap:true to simulate UI cap.
        services.AddBitmaskSolverServices(enableCap);

        // Test dispatcher (synchronous execution)
        services.AddSingleton<IDispatcher, TestDispatcher>();

        // Dialog service mock
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // Formatter (will override the TryAdd from solver registration if already present)
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // ViewModel under test
        services.AddTransient<MainViewModel>();

        return services;
    }

    public static ServiceProvider BuildTestServiceProvider(bool enableCap = false) =>
        new ServiceCollection()
            .AddViewModelTestServices(enableCap)
            .BuildServiceProvider();

    // Backward compatible aliases
    public static ServiceProvider InitializeForTests(bool enableCap = false) =>
        BuildTestServiceProvider(enableCap);

    public static ServiceProvider InitializeForTestsWithMock(ISolver mockSolver, bool enableCap = false)
    {
        var services = new ServiceCollection();

        services.AddViewModelTestServices(enableCap);

        // Override with supplied mock for all solver interfaces
        services.AddSingleton(mockSolver);
        services.AddSingleton<ISolver>(mockSolver);
        services.AddSingleton<ISolverBackEnd>(mockSolver);
        services.AddSingleton<ISolverFrontEnd>(mockSolver);

        return services.BuildServiceProvider();
    }
}