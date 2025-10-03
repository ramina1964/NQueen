namespace NQueen.GUI.Infrastructure;

public static class GuiServiceCollectionExtensions
{
    public static IServiceCollection AddGuiServices(this IServiceCollection services, bool enableCap = true)
    {
        services.AddSingleton<IDispatcher, WpfDispatcher>();
        services.AddTransient<ISaveFileDialogService, SaveFileDialogService>();
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Windows / ViewModels
        services.AddSingleton<MainWindow>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();

        // Solver registration:
        // Register the concrete AND map the interface so MainViewModel(ISolver ...) resolves.
        services.AddTransient(sp => new BitmaskSolver(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap
            )
        );
        services.AddTransient<ISolver>(sp => sp.GetRequiredService<BitmaskSolver>());

        return services;
    }

    public static IServiceProvider BuildGuiServiceProvider(bool enableCap = true) =>
        new ServiceCollection()
            .AddGuiServices(enableCap)
            .BuildServiceProvider();

    // Backward compatible entry point (retain existing call sites if any)
    public static IServiceProvider Initialize() => BuildGuiServiceProvider();
}
