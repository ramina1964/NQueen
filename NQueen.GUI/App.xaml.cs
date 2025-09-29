namespace NQueen.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            Debug.WriteLine("[FirstChance] " + args.Exception);

        base.OnStartup(e);

        _serviceProvider = GuiServiceCollectionExtensions.BuildGuiServiceProvider();
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }

    private IServiceProvider _serviceProvider = null!;
}
