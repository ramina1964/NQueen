using NQueen.NextGenKernel.Solvers;

namespace NQueen.ConsoleApp.Services;

public static class ServiceRegistration
{
    public static void AddNQueenServices(this IServiceCollection services)
    {
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<ISolverBackEnd, SimulationOrchestrator>();
    }
}
