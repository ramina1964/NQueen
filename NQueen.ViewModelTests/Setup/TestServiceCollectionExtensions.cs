namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    public static IServiceProvider InitializeForTests()
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddSingleton<IDispatcher, TestDispatcher>();
        services.AddSingleton<ISaveFileDialogService, MockSaveFileDialogService>();

        // Shared NQueen-Related Services
        services.AddNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<ChessboardViewModel>();
        services.AddTransient<ChessboardUserControl>();
        services.AddSingleton<InputPanelUserControl>();
        services.AddSingleton<SimulationPanelUserControl>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();

        // Build the service provider
        return services.BuildServiceProvider();
    }
}