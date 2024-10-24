namespace NQueen.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services
                    .AddTransient<SolutionUpdateDTO>()
                    .AddTransient<ISolutionManager, SolutionManager>()
                    .AddTransient<ISolver, BackTracking>()
                    .AddTransient<MainViewModel>()
                    .AddTransient<MainView>();
            });

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var frm = services.GetRequiredService<MainView>();
            frm.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error has occurred, {ex.Message}");
        }
    }
}
