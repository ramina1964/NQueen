namespace NQueen.KernelBitmask.Services;

public static class BitmaskCollectionExtensions
{
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool enableCap = true,
        int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        services.AddTransient<BitmaskSolverExtended>(sp =>
            new BitmaskSolverExtended(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap: enableCap));

        services.AddTransient<ISolverPruning, BitmaskSolverExtended>();
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverFrontEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());

        return services;
    }
}
