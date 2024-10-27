namespace NQueen.Kernel;

public static class ServiceRegistration
{
    public static void AddNQueenServices(this IServiceCollection services)
    {
        services.AddScoped<ISolutionManager, SolutionManager>();
        services.AddScoped<ISolver, BackTrackingSolver>();
    }
}
