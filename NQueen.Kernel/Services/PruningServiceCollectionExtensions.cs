namespace NQueen.Kernel.Services;

public static class PruningServiceCollectionExtensions
{
    public static void NQueenServices(this IServiceCollection services)
    {
        // Register BoardState as a factory function
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));

        // Register SolutionFormatter
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register SolverEngine as both itself and ISolver
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver, SolverEngine>();

        // Register SimulationOrchestrator
        services.AddTransient<SimulationOrchestrator>();

        // Register ISolverBackEnd as the same instance as ISolver
        services.AddTransient<ISolverBackEnd>(sp => sp.GetRequiredService<ISolver>());
    }
}