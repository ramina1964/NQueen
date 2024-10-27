namespace NQueen.ConsoleApp;

// In order to enable dotnet-counters you need to install dotnet-counters tool with the
// following command (use cmd) dotnet tool install --global dotnet-counters
// link: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters#:~:text=dotnet-counters%20is%20a%20performance%20monitoring%20tool%20for%20ad-hoc,values%20that%20are%20published%20via%20the%20EventCounter%20API.

public class Program
{
    public static void Main(string[] args)
    {
        using var serviceProvider = ConfigureServices();
        var app = serviceProvider.GetRequiredService<App>();
        app.Run(args);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Specific dependencies for the Console App
        services.AddTransient<DispatchCommands>();
        services.AddTransient<IConsoleUtils, ConsoleUtils>();
        services.AddSingleton<App>();

        // Register shared services
        services.AddNQueenServices();

        return services.BuildServiceProvider();
    }
}
