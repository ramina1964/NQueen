namespace NQueen.UnitTests.Setup;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register application services
        services.NQueenServices();

        // Todo: Adjust this line to use NQuenn-Kernel.SolverEngine, instead
        services.AddScoped<ISolverPruning, SolverEngine>();
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        return services;
    }

    // Todo: Consider adding test-specific services here, or removing it.
    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // Register test-specific services
        return services;
    }
}
