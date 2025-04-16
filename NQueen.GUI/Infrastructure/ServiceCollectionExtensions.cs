namespace NQueen.GUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();

        // IDispatcher for WPF
        services.AddSingleton<IDispatcher, WpfDispatcher>();

        // Shared NQueen-Related Services
        services.AddNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<Chessboard>();
        services.AddTransient<ChessboardUserControl>();
        services.AddSingleton<InputPanelUserControl>();
        services.AddSingleton<SimulationPanelUserControl>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();

        // Build the service provider
        return services.BuildServiceProvider();
    }

    public static IServiceProvider InitializeForTests()
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddSingleton<IDispatcher, TestDispatcher>();

        // Shared NQueen-Related Services
        services.AddNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<Chessboard>();
        services.AddTransient<ChessboardUserControl>();
        services.AddSingleton<InputPanelUserControl>();
        services.AddSingleton<SimulationPanelUserControl>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();

        // Build the service provider
        return services.BuildServiceProvider();
    }
}
