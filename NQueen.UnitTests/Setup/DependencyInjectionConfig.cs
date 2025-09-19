namespace NQueen.UnitTests.Setup;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register application services (tests want full solution sets -> disableCap = true)
        services.AddBitmaskSolverServices(disableCap: true);

        // Register test solution formatter (overrides default if necessary)
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
