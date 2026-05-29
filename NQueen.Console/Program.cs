namespace NQueen.ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Non-interactive fast path when flags supplied (skip menu)
        if (args.Length > 0 && HasSolverArgs(args))
        {
            RunNonInteractive(args);
            return;
        }

        using var serviceProvider = ConfigureServices();
        var app = serviceProvider.GetRequiredService<App>();
        await app.Run(args); // interactive menu
    }

    private static bool HasSolverArgs(string[] args)
    {
        // Treat presence of any recognized flag as non-interactive intent
        return args.Any(a => a.StartsWith("--mode", StringComparison.OrdinalIgnoreCase)
                          || a.StartsWith("--size", StringComparison.OrdinalIgnoreCase)
                          || a.Equals("--count-only", StringComparison.OrdinalIgnoreCase)
                          || a.Equals("--halfboard", StringComparison.OrdinalIgnoreCase)
                          || a.Equals("--help", StringComparison.OrdinalIgnoreCase));
    }

    private static void RunNonInteractive(string[] args)
    {
        // Defaults
        var mode = SolutionMode.All;
        int size = 8;
        bool countOnly = false;
        bool halfBoard = false;
        int displayedCap = SimulationSettings.MaxDisplayedCount;

        // Simple linear parse
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].Trim();
            switch (arg.ToLowerInvariant())
            {
                case "--help":
                case "-h":
                    PrintHelp();
                    return;
                case "--mode":
                    if (i + 1 < args.Length)
                    {
                        var val = args[++i].Trim().ToLowerInvariant();
                        mode = val switch
                        {
                            "all" => SolutionMode.All,
                            "unique" => SolutionMode.Unique,
                            "single" => SolutionMode.Single,
                            _ => mode
                        };
                    }
                    break;
                case "--size":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) size = n;
                    break;
                case "--count-only":
                    countOnly = true;
                    displayedCap = 0; // suppress materialization
                    break;
                case "--materialize":
                    countOnly = false;
                    displayedCap = SimulationSettings.MaxDisplayedCount;
                    break;
                case "--halfboard":
                    halfBoard = true;
                    break;
            }
        }

        // Formatter
        if (halfBoard && mode != SolutionMode.All)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: --halfboard is only supported for --mode all. Flag ignored for mode '{mode}'.");
            Console.ResetColor();
            halfBoard = false;
        }

        var formatter = new SolutionFormatter();
        using var solver = new BitmaskSolver(size, mode, DisplayMode.Hide, formatter, maxSolutionsInOutput: displayedCap)
        {
            EnableEvents = false,
            IsSolverCanceled = false,
            UseCountOnlyAllMode = countOnly && mode == SolutionMode.All,
            UseCountOnlyUniqueMode = countOnly && mode == SolutionMode.Unique,
            EnableHalfBoardRestriction = halfBoard && mode == SolutionMode.All,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            UseAdaptiveDepth = size >= 14,
        };
        var results = solver.Solve();

        Console.WriteLine("NQueen Solver (non-interactive)");
        Console.WriteLine($"  Mode            : {mode}");
        Console.WriteLine($"  Board Size      : {size}");
        Console.WriteLine($"  Half-Board Flag : {(halfBoard && mode == SolutionMode.All ? "ON" : "OFF")}");
        Console.WriteLine($"  Count-Only      : {countOnly}");
        Console.WriteLine($"  Solutions Count : {results.SolutionsCount:N0}");
        Console.WriteLine($"  Elapsed (sec)   : {results.ElapsedTimeInSec}");
        if (!countOnly && results.Solutions.Count > 0)
        {
            Console.WriteLine($"  Displayed ({results.Solutions.Count}):");
            foreach (var sol in results.Solutions)
                Console.WriteLine($"    {sol.Name}: {sol.Details}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project NQueen.Console -- [options]\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --mode <all|unique|single>    Solution mode (default: all)");
        Console.WriteLine("  --size <N>                     Board size (default: 8)");
        Console.WriteLine("  --count-only                   Count solutions only (no materialization)");
        Console.WriteLine("  --materialize                  Materialize sample solutions (default if --count-only omitted)");
        Console.WriteLine("  --halfboard                    Enable half-board restriction (All mode only, N>=15)");
        Console.WriteLine("  --help                         Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Count All solutions N=15 with half-board: dotnet run --project NQueen.Console -- --mode all --size 15 --count-only --halfboard");
        Console.WriteLine("  Materialize 5 sample Unique solutions N=12: dotnet run --project NQueen.Console -- --mode unique --size 12");
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core application registrations
        services.AddNQueenServices(enableCap: true);

        // Root app
        services.AddSingleton<App>();

        return services.BuildServiceProvider();
    }
}
