namespace NQueen.Kernel.Services;

public static class PruningServiceCollectionExtensions
{
    public static void NQueenServices(this IServiceCollection services)
    {
        // Register BoardState as a factory function
        services.AddTransient<Func<int, BoardState>>(
            sp => size => new BoardState(size));

        // Register SolutionFormatter
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register SolverEngine as itself and its interfaces
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolverPruning, SolverEngine>();

        // Register SimulationOrchestrator as ISolverFrontEndPruning
        services.AddTransient<ISolverPruning, SolverOrchestrator>();
    }
}