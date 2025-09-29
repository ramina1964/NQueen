namespace NQueen.UnitTests.Setup;

public static class UnitTestServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register solver + related interfaces (full set: no cap in tests).
        services.AddBitmaskSolverServices(enableCap: false);

        // Override default formatter with test implementation (last registration wins).
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();
        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // Placeholder for additional per-test registrations.
        return services;
    }
}
