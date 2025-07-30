namespace NQueen.UnitTests.Setup;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services
        services.AddNextGenNQueenServices();
        services.AddScoped<ISolverBackEnd, SimulationOrchestrator>();

        return services;
    }

    // Todo: Consider adding test-specific services here, or removing it.
    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // Register test-specific services (if any)
        // Example: Mock services or test utilities
        
        return services;
    }
}
