namespace NQueen.GUI.Infrastructure;

public static class NQueenViewModels
{
    public static void AddNQueenViewModels(this IServiceCollection services)
    {
        services.AddTransient<ChessboardViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
