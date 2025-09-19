namespace NQueen.KernelBitmask.Services;

using NQueen.Domain.Interfaces;
using NQueen.KernelBitmask.Solvers;

public static class BitmaskCollectionExtensions
{
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool disableCap = false)
    {
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();
        if (disableCap)
        {
            services.AddTransient<BitmaskSolverExtended>(sp => new BitmaskSolverExtended(sp.GetRequiredService<ISolutionFormatter>(), disableCap: true));
        }
        else
        {
            services.AddTransient<BitmaskSolverExtended>();
        }
        services.AddTransient<ISolverBackEndPruning>(sp => sp.GetRequiredService<BitmaskSolverExtended>());
        return services;
    }
}
