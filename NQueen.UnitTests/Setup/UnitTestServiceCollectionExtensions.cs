namespace NQueen.UnitTests.Setup;

public static class UnitTestServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Shared uncapped solver registration for all solver interfaces
        services.AddTransient<BitmaskSolver>(sp =>
            new BitmaskSolver(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap: false // disable output cap for tests
            )
        );

        services.AddTransient<ISolver>(sp => sp.GetRequiredService<BitmaskSolver>());
        services.AddTransient<ISolverBackEnd>(sp => sp.GetRequiredService<BitmaskSolver>());

        // Test formatter (after solver dependencies)
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();

        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        // No additional solver registration here to avoid reintroducing cap.
        return services;
    }
}
