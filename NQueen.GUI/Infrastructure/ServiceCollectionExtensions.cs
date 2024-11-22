namespace NQueen.GUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();

        // Register shared services
        services.AddNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<Chessboard>();
        services.AddTransient<ChessboardUserControl>();
        services.AddSingleton<InputPanelUserControl>();
        services.AddSingleton<SimulationPanelUserControl>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();

        return services.BuildServiceProvider();
    }
}
