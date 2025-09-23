namespace NQueen.KernelBitmask.Services;

public static class BitmaskCollectionExtensions
{
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool disableCap = false,
        int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        if (disableCap)
            services.AddTransient(sp =>
                new BitmaskSolverExtended(
                    sp.GetRequiredService<ISolutionFormatter>(),
                    maxSolutionsInOutput: 0)); // 0 disables cap
        else
            services.AddTransient<BitmaskSolverExtended>(sp =>
                new BitmaskSolverExtended(
                    sp.GetRequiredService<ISolutionFormatter>(),
                    maxSolutionsInOutput: maxSolutionsInOutput));

        // Register all needed abstractions to same implementation
        services.AddTransient<ISolverPruning, BitmaskSolverExtended>();
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverFrontEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());

        return services;
    }
}
