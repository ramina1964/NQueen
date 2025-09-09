namespace NQueen.Kernel.Services;

public static class PruningServiceCollectionExtensions
{
    public static void NQueenServices(this IServiceCollection services)
    {
        // Register BoardState as a factory function
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));

        // Register SolutionFormatter
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register SolverEngine as both itself and ISolverPruning
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolverPruning, SolverEngine>();

        // Register SimulationOrchestrator
        services.AddTransient<SimulationOrchestrator>();

        // Register ISolverBackEnd as the same instance as ISolverPruning
        services.AddTransient<ISolverBackEnd>(sp => sp.GetRequiredService<ISolverPruning>());
    }
}