namespace NQueen.NextGenKernel.Services;

public static class NextGenServiceCollectionExtensions
{
    public static void NQueenServices(this IServiceCollection services)
    {
        // Register BoardState as a factory function
        services.AddTransient<Func<int, BoardState>>(sp => size =>
            new BoardState(size));

        // Register SolutionManager and SolutionFormatter
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register SolverEngine as itself and ISolver
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver, SolverEngine>();

        // Register SimulationOrchestrator as ISolver
        services.AddTransient<ISolver, SimulationOrchestrator>();
    }
}
