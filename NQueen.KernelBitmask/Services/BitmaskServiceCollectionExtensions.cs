namespace NQueen.KernelBitmask.Services;

public static class BitmaskServiceCollectionExtensions
{
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool enableCap = true,
        int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        // Formatting (can be overridden by tests with another registration later)
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Register the concrete solver once. All interface registrations below resolve this same instance
        // so a single transient object participates in event wiring within a resolution graph.
        services.AddTransient<BitmaskSolverExtended>(sp =>
            new BitmaskSolverExtended(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap: enableCap));

        // IMPORTANT: Use factory delegates to reuse the same BitmaskSolverExtended instance per resolution.
        services.AddTransient<ISolverPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverFrontEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());

        return services;
    }
}
