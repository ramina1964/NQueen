namespace NQueen.NextGenKernel.Services;

public static class NQueenServiceCollectionExtensions
{
    public static void AddNextGenNQueenServices(this IServiceCollection services)
    {
        services.AddTransient<Func<int, BoardState>>(sp => size => new BoardState(size));
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();
        services.AddTransient<SolverEngine>();
        services.AddTransient<ISolver, SolverEngine>();
        services.AddTransient<ISolverBackEnd>(sp => sp.GetRequiredService<ISolver>());
        services.AddTransient<SimulationOrchestrator>();
    }
}
