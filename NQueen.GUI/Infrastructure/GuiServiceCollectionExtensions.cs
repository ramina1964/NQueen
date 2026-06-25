namespace NQueen.GUI.Infrastructure;

/// <summary>
/// GUI (WPF) DI registration.
/// Provides dispatcher, dialog services, ViewModels, windows, and the solver.
/// </summary>
public static class GuiServiceCollectionExtensions
{
    public static IServiceCollection AddGuiServices(this IServiceCollection services, bool enableCap = true)
    {
        // UI infra
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();

        // Solver (capped for UI responsiveness by default)
        services.AddBitmaskSolverServices(enableCap);

        // Windows / Views / ViewModels
        services.AddSingleton<MainWindow>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChessboardView>();
        services.AddTransient<InputPanel>();
        services.AddTransient<SimulationPanel>();

        return services;
    }

    public static IServiceProvider BuildGuiServiceProvider(bool enableCap = true) =>
        new ServiceCollection()
            .AddGuiServices(enableCap)
            .BuildServiceProvider();

    // Legacy alias if older code still calls Initialize()
    public static IServiceProvider Initialize() => BuildGuiServiceProvider();
}
