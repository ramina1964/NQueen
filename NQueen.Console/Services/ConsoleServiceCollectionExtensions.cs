namespace NQueen.ConsoleApp.Services;

public static class ConsoleServiceCollectionExtensions
{
    public static IServiceCollection AddNQueenServices(this IServiceCollection services, bool enableCap = true)
    {
        services.AddTransient<ISolutionFormatter, SolutionFormatter>();

        // Stateless factory delegate for BitmaskBoardState creation.
        services.AddTransient<Func<int, BitmaskBoardState>>(sp => size => BitmaskBoardState.Create(size));

        return services;
    }
}
