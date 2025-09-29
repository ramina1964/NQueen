namespace NQueen.ConsoleApp.Services;

public static class ServiceRegistration
{
    // Registers only the active Bitmask kernel services (legacy kernels removed).
    public static IServiceCollection AddNQueenServices(this IServiceCollection services, bool enableCap = true)
    {
        // Bitmask solver + formatter + pruning interfaces
        services.AddBitmaskSolverServices(enableCap);

        // Factory for creating BitmaskBoardState instances
        services.AddTransient<Func<int, BitmaskBoardState>>(
            _ => size => BitmaskBoardState.Create(size));

        return services;
    }
}
