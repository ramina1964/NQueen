namespace NQueen.ConsoleApp.Services;

/// <summary>
/// Console host DI registrations.
/// Focuses on lightweight services needed for command dispatch & solver usage.
/// </summary>
public static class ConsoleServiceCollectionExtensions
{
    public static IServiceCollection AddNQueenServices(this IServiceCollection services, bool enableCap = true)
    {
        // Core solver + formatter (capped in console for readability; change enableCap as needed)
        services.AddBitmaskSolverServices(enableCap);

        // Additional factories (example: board state factory)
        services.AddTransient<Func<int, BitmaskBoardState>>(_ => size => BitmaskBoardState.Create(size));

        return services;
    }
}
