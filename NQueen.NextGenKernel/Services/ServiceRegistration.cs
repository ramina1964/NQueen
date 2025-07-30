namespace NQueen.NextGenKernel.Services;

public static class ServiceRegistration
{
    //public class SolverEngine(
    //  ISolutionManager solutionManager,
    //  Func<int, BoardState> boardStateFactory)

    public static void AddNextGenNQueenServices(this IServiceCollection services)
    {
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver>(sp => sp.GetRequiredService<SolverEngine>());
        services.AddTransient<SimulationOrchestrator>();
    }
}
