namespace NQueen.ConsoleApp.Services;

public static class ServiceRegistration
{
    /// <summary>
    /// Registers N-Queen console application services (bitmask solver + helpers).
    /// </summary>
    public static IServiceCollection AddNQueenServices(this IServiceCollection services, bool enableCap = true)
    {
        services.AddBitmaskSolverServices(enableCap);

        // Stateless factory delegate for BitmaskBoardState creation.
        services.AddTransient<Func<int, BitmaskBoardState>>(sp => size => BitmaskBoardState.Create(size));

        return services;
    }
}
