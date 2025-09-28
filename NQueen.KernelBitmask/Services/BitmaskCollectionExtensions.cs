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
        {
            // Use overload that sets internal _disableCap flag so ShouldAddSolution() bypasses cap
            services.AddTransient<BitmaskSolverExtended>(sp =>
                new BitmaskSolverExtended(sp.GetRequiredService<ISolutionFormatter>(), disableCap: true));
        }
        else
        {
            services.AddTransient<BitmaskSolverExtended>(sp =>
                new BitmaskSolverExtended(
                    sp.GetRequiredService<ISolutionFormatter>(),
                    maxSolutionsInOutput: maxSolutionsInOutput));
        }

        services.AddTransient<ISolverPruning, BitmaskSolverExtended>();
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverFrontEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());

        return services;
    }
}
