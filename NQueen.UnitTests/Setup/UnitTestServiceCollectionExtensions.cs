namespace NQueen.UnitTests.Setup;

/// <summary>
/// DI setup for pure unit tests (backend solver focus).
/// Registers an uncapped solver and replaces the default formatter with a test formatter.
/// </summary>
public static class UnitTestServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Uncapped solver to ensure all solutions are enumerated
        services.AddBitmaskSolverForTests();

        // Override the default solution formatter with the test-specific one
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // (Intentionally left blank; place per-test overrides/mocks here if needed)
        return services;
    }
}
