namespace NQueen.GUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();

        // IDispatcher for WPF
        services.AddSingleton<IDispatcher, WpfDispatcher>();

        // Shared NQueen-Related Services
        services.AddNextGenNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<ChessboardViewModel>();
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();

        // Build the service provider
        return services.BuildServiceProvider();
    }
}
