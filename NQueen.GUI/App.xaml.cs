namespace NQueen.GUI;

// Todo: Address the accessibility errors, then remove this line from the project file:
// <NoWarn>CS0618,CS0168</NoWarn>

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainView>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISolutionManager, SolutionManager>();
        services.AddSingleton<ISolver, BackTrackingSolver>();
        services.AddSingleton<ICommandManager, CommandManager>();
        services.AddSingleton(provider =>
        {
            var solver = provider.GetRequiredService<ISolver>();
            var commandManager = provider.GetRequiredService<ICommandManager>();
            var mainViewModel = new MainViewModel(solver, commandManager);
            return mainViewModel;
        });

        services.AddSingleton<SolutionUpdateDTO>();
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();
        services.AddTransient<MainView>();
    }

    private IServiceProvider _serviceProvider;
}
