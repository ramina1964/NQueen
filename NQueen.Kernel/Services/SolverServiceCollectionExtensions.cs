namespace NQueen.Kernel.Services;

/// <summary>
/// Centralized DI registration helpers for the Bitmask solver.
/// Use these helpers across Console, GUI, and test projects to avoid duplication.
/// </summary>
public static class SolverServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Bitmask solver and its interface mappings.
    /// Also (Try) registers a default <see cref="ISolutionFormatter"/> if one was not already provided.
    /// You can override the formatter after calling this (e.g. with a test-specific implementation).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="enableCap">
    /// True to enforce solution output capping (UI/runtime scenarios),
    /// False for full result enumeration (tests / benchmarking).
    /// </param>
    /// <param name="solverLifetime">Lifetime for the solver + interface mappings (default Transient).</param>
    public static IServiceCollection AddBitmaskSolverServices(
        this IServiceCollection services,
        bool enableCap = true,
        ServiceLifetime solverLifetime = ServiceLifetime.Transient)
    {
        // Provide a formatter only if one is not already registered (tests / GUI may override later).
        services.TryAddSingleton<ISolutionFormatter, SolutionFormatter>();

        // Register the concrete solver with the chosen lifetime.
        var descriptor = new ServiceDescriptor(
            typeof(BitmaskSolver),
            sp => new BitmaskSolver(
                sp.GetRequiredService<ISolutionFormatter>(),
                enableCap: enableCap),
            solverLifetime);

        services.Add(descriptor);

        // Map all relevant solver interfaces to the same concrete instance per lifetime.
        services.Add(new ServiceDescriptor(typeof(ISolver),
            sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));
        services.Add(new ServiceDescriptor(typeof(ISolverBackEnd),
            sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));
        services.Add(new ServiceDescriptor(typeof(ISolverFrontEnd),
            sp => sp.GetRequiredService<BitmaskSolver>(), solverLifetime));

        return services;
    }

    /// <summary>
    /// Convenience helper for test environments: registers an uncapped solver (all solutions returned).
    /// </summary>
    public static IServiceCollection AddBitmaskSolverForTests(this IServiceCollection services) =>
        services.AddBitmaskSolverServices(enableCap: false, solverLifetime: ServiceLifetime.Transient);
}