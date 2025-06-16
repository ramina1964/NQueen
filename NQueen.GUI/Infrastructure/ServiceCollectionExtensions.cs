namespace NQueen.GUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();

        // WPF-specific services
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();

        // Shared NQueen-Related Services
        services.AddNextGenNQueenServices();

        // Register shared ViewModels
        services.AddNQueenViewModels();

        // Register specific services, i.e., views and view models
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();

        services.AddSingleton<MainWindow>();

        // Build the service provider
        return services.BuildServiceProvider();
    }
}
