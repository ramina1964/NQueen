namespace NQueen.GUI.Infrastructure;

public static class GuiServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();
        services.AddBitmaskSolverServices(disableCap: true);

        // Register MainWindow for DI
        services.AddSingleton<MainWindow>();

        // Register MainViewModel for DI
        services.AddTransient<MainViewModel>();

        // Register User Controls for DI
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();

        // Other GUI-specific registrations...
        return services.BuildServiceProvider();
    }
}
