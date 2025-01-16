namespace NQueen.Kernel.Services;

public static class ServiceRegistration
{
    public static void AddNQueenCommonServices(this IServiceCollection services)
    {
        services.AddScoped<ISolutionManager, SolutionManager>();
        services.AddTransient<SolutionUpdateDTO>();
        services.AddScoped<ISolver, BackTrackingSolver>();
    }
}
