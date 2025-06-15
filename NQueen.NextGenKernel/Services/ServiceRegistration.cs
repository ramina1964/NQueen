namespace NQueen.NextGenKernel.Services;

public static class ServiceRegistration
{
    public static void AddNextGenNQueenServices(this IServiceCollection services)
    {
        // Register the main solver
        services.AddTransient<ISolver, SimulationOrchestrator>();
        services.AddTransient<ISolutionManager, SolutionManager>();

        // Register supporting classes if you want to inject them directly elsewhere
        services.AddTransient<BoardState>();
        services.AddTransient<SolverCancellation>();
        services.AddTransient<SolverEngine>();
    }
}
