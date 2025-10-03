namespace NQueen.UnitTests.Setup;

public static class UnitTestServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Solver (no cap in tests)
        services.AddTransient<BitmaskSolver>(sp =>
            new BitmaskSolver(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap: false   // unlimited for tests
            )
        );
        services.AddTransient<ISolver>(sp => sp.GetRequiredService<BitmaskSolver>());

        // Test formatter overrides any default (register after solver dependencies if needed)
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // Additional per-test registrations can go here.
        services.AddTransient<ISolverBackEnd, BitmaskSolver>();
        return services;
    }
}
