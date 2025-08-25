namespace NQueen.GUI.Infrastructure;

public static class GuiServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();
        services.AddNextGenNQueenServices();

        // Register MainWindow for DI
        services.AddSingleton<MainWindow>();

        // Register MainViewModel for DI
        services.AddTransient<MainViewModel>();

        // Register User Controls for DI
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();

        // Register Default formatter
        services.AddSingleton<ISolutionFormatter, DefaultSolutionFormatter>();

        // Other GUI-specific registrations...
        return services.BuildServiceProvider();
    }
}
