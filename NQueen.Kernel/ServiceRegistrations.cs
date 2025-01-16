namespace NQueen.Kernel;

public static class ServiceRegistration
{
    public static void AddNQueenCommonServices(this IServiceCollection services)
    {
        // Register the services related to the NQueen.Kernel project
        services.AddTransient<SolutionUpdateDTO>();
        services.AddSingleton<ISolutionManager, SolutionManager>();
        services.AddSingleton<ISolver, BackTrackingSolver>();
    }
}