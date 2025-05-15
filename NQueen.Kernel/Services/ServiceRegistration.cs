namespace NQueen.Kernel.Services;

public static class ServiceRegistration
{
    public static void AddNQueenServices(this IServiceCollection services)
    {
        services.AddSingleton<ISolutionManager, SolutionManager>();
        services.AddSingleton<ISolver, BackTrackingSolver>();
    }
}
