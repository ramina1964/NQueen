namespace NQueen.GUI.Infrastructure;

public static class GuiServiceCollectionExtensions
{
    /// <summary>
    /// Registers GUI-level services and view models. Returns the collection for chaining.
    /// </summary>
    public static IServiceCollection AddGuiServices(this IServiceCollection services, bool enableCap = true)
    {
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();
        services.AddBitmaskSolverServices(enableCap: enableCap); // GUI uses capped solutions by default

        // Windows / ViewModels
        services.AddSingleton<MainWindow>();
        services.AddTransient<MainViewModel>();

        // User controls (transient - lightweight)
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();
        return services;
    }

    /// <summary>
    /// Convenience bootstrap used by App.xaml.cs.
    /// </summary>
    public static IServiceProvider BuildGuiServiceProvider(bool enableCap = true) =>
        new ServiceCollection()
            .AddGuiServices(enableCap)
            .BuildServiceProvider();

    // Backward compatible entry point (retain existing call sites if any)
    public static IServiceProvider Initialize() => BuildGuiServiceProvider();
}
