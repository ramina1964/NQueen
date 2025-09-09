namespace NQueen.NextGenKernel.Services;

public static class NextGenServiceCollectionExtensions
{
    public static void NQueenServices(this IServiceCollection services)
    {
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register SolverEngine as both itself and ISolver (or ISolverPruning if updated)
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver, SolverEngine>();

        // Register ISolverBackEnd as the same instance as ISolver
        services.AddTransient<ISolverBackEnd>(sp => sp.GetRequiredService<ISolver>());

        // Register SimulationOrchestrator
        services.AddTransient<SimulationOrchestrator>();
    }
}
