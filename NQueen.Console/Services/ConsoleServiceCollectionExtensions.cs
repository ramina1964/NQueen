namespace NQueen.ConsoleApp.Services;

public static class ConsoleServiceCollectionExtensions
{
    public static IServiceCollection AddNQueenServices(this IServiceCollection services, bool enableCap = true)
    {
        // Conditional registration of formatter (only if not already added)
        bool hasFormatter = services.Any(sd => sd.ServiceType == typeof(ISolutionFormatter));
        if (!hasFormatter)
        {
            services.AddSingleton<ISolutionFormatter, SolutionFormatter>();
        }

        // Register BitmaskSolver with console-specific default storage modes (CountOnly)
        var solverLifetime = ServiceLifetime.Transient;
        services.Add(new ServiceDescriptor(
            typeof(BitmaskSolver),
            sp =>
            {
                var solver = new BitmaskSolver(sp.GetRequiredService<ISolutionFormatter>(), enableCap: enableCap)
                {
                    AllStorageMode = ResultStorageMode.CountOnly,
                    UniqueStorageMode = ResultStorageMode.CountOnly
                };
                return solver;
            },
            solverLifetime));

        // Map interfaces to the solver instance
        services.Add(new ServiceDescriptor(typeof(ISolver), sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));
        services.Add(new ServiceDescriptor(typeof(ISolverBackEnd), sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));
        services.Add(new ServiceDescriptor(typeof(ISolverFrontEnd), sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));

        return services;
    }
}
