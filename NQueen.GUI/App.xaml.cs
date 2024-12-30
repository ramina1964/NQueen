namespace NQueen.GUI;

// Todo: Address the accessibility errors, then remove this line from the project file:
// <NoWarn>CS0618,CS0168</NoWarn>

public partial class App : Application
{
    private IServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainView>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();
        services.AddTransient<MainView>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
