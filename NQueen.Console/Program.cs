namespace NQueen.ConsoleApp;

// In order to enable dotnet-counters you need to install dotnet-counters tool with the
// following command (use cmd) dotnet tool install --global dotnet-counters
// link: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters#:~:text=dotnet-counters%20is%20a%20performance%20monitoring%20tool%20for%20ad-hoc,values%20that%20are%20published%20via%20the%20EventCounter%20API.

public class Program
{
    public static void Main(string[] args)
    {
        // The "using" keyword in the following two lines helps disposing of resources properly.
        using IHost host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;
        try
        {
            services.GetService<App>()?.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error has occurred: {ex.Message}");
            Console.ReadLine();
        }

        // Todo: Put the methods below inside the App class.
        // Todo: You need to change to font to SimSun-ExtB in order to show unicode characters in console - IMPORTANT
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        DispatchCommands.InitCommands();
        DispatchCommands.OutputBanner();
        DispatchCommands.LaunchConsoleMonitor();

        if (args.Length == 0)
            DispatchCommands.ProcessCommandsInteractively();
        else
            DispatchCommands.ProcessCommandsFromArgs(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            services
                .AddTransient<SolutionUpdateDTO>()
                .AddTransient<ISolutionDeveloper, SolutionDeveloper>()
                .AddTransient<ISolver, BackTracking>()
                .AddTransient<App>();
        });
}
