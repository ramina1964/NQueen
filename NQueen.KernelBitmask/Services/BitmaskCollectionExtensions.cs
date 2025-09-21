namespace NQueen.KernelBitmask.Services;

public static class BitmaskCollectionExtensions
{
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool disableCap = false)
    {
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        if (disableCap)
            services.AddTransient(sp =>
                new BitmaskSolverExtended(sp.GetRequiredService<ISolutionFormatter>(), disableCap: true));
        else
            services.AddTransient<BitmaskSolverExtended>();

        // Register all needed abstractions to same implementation
        services.AddTransient<ISolverPruning, BitmaskSolverExtended>();
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        services.AddTransient<ISolverFrontEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());

        return services;
    }
}
