namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for MainViewModel unit tests.
    /// </summary>
    /// <param name="enableCap">Pass false to disable solution capping in tests.</param>
    public static IServiceCollection AddViewModelTestServices(
        this IServiceCollection services,
        bool enableCap = false)
    {
        // Bitmask solver (registers ISolutionFormatter + ISolverPruning & related fronts/backs)
        //services.AddBitmaskSolverServices(enableCap);

        // Test dispatcher (synchronous)
        services.AddSingleton<IDispatcher, TestDispatcher>();

        // Mock save dialog / file service used by MainViewModel.Save()
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // ViewModel under test
        services.AddTransient<MainViewModel>();

        return services;
    }

    public static ServiceProvider BuildTestServiceProvider(bool enableCap = false) =>
        new ServiceCollection()
            .AddViewModelTestServices(enableCap)
            .BuildServiceProvider();

    // ---------------- Legacy helper method names still used in TestHelpers ----------------

    /// <summary>
    /// Backward-compatible factory used by existing tests (maps to BuildTestServiceProvider).
    /// </summary>
    public static ServiceProvider InitializeForTests(bool enableCap = false) =>
        BuildTestServiceProvider(enableCap);

    /// <summary>
    /// Initializes DI for tests while injecting a provided mock ISolverPruning.
    /// Ensures other required services are present.
    /// </summary>
    public static ServiceProvider InitializeForTestsWithMock(ISolver mockSolver, bool enableCap = false)
    {
        var services = new ServiceCollection();

        // Register standard test services (including real solver) first.
        services.AddViewModelTestServices(enableCap);

        // Override solver with supplied mock (register for all relevant interfaces if needed).
        services.AddSingleton(mockSolver);
        services.AddSingleton<ISolver>(mockSolver);
        services.AddSingleton<ISolverBackEnd>(mockSolver);
        services.AddSingleton<ISolverFrontEnd>(mockSolver);

        return services.BuildServiceProvider();
    }
}