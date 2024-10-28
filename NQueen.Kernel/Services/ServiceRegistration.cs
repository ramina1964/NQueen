using NQueen.Kernel.Interfaces;
using NQueen.Kernel.Solvers;

namespace NQueen.Kernel.Services;

public static class ServiceRegistration
{
    public static void AddNQueenServices(this IServiceCollection services)
    {
        services.AddScoped<ISolutionManager, SolutionManager>();
        services.AddScoped<ISolver, BackTrackingSolver>();
    }
}
