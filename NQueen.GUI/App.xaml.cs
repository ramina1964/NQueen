namespace NQueen.GUI;

// Todo: Address the accessibility errors, then remove this line from the project file:
// <NoWarn>CS0618,CS0168</NoWarn>

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var mainView = new MainView(mainViewModel, _serviceProvider);
        mainView.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register the services related to the NQueen.GUI project
        services.AddSingleton<ICommandManager, CommandManager>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ChessboardViewModel>();
        services.AddTransient<InputPanelUserControl>();
        services.AddTransient<SimulationPanelUserControl>();
        services.AddTransient<ChessboardUserControl>();
        services.AddTransient<SelectedSolutionUserControl>();
        services.AddTransient<SolutionSummaryUserControl>();
        services.AddTransient<SolutionListUserControl>();

        // Register InputValidator and InputViewModel
        services.AddSingleton<InputValidator>();
        services.AddSingleton<InputViewModel>();

        // Register shared services
        services.AddNQueenCommonServices();
    }

    private IServiceProvider _serviceProvider;
}

