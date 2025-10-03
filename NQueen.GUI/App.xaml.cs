namespace NQueen.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
                Debug.WriteLine("[FirstChance] " + args.Exception);

            base.OnStartup(e);

            _serviceProvider = GuiServiceCollectionExtensions.BuildGuiServiceProvider();
            _serviceProvider.GetRequiredService<MainWindow>().Show();
        }
        catch (Exception)
        {
            throw new Exception("Exception under registering dependencies!");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }

    private IServiceProvider _serviceProvider = null!;
}
