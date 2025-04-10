namespace NQueen.UnitTests.Setup;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services
        services.AddNQueenServices();
        services.AddScoped<ISolverBackEnd, BackTrackingSolver>();

        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // Register test-specific services (if any)
        // Example: Mock services or test utilities
        return services;
    }
}
