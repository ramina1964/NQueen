namespace NQueen.Kernel.Services;

public static class ServiceRegistration
{
    public static void AddNQueenServices(this IServiceCollection services)
    {
        services.AddTransient<ISolutionManager, SolutionManager>();
        services.AddTransient<ISolver, BackTrackingSolver>();
    }
}
