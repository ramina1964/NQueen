namespace NQueen.GUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider Initialize()
    {
        var serviceCollection = new ServiceCollection();

        // Register core servicess
        serviceCollection.AddSingleton<ISolver, BackTrackingSolver>();
        serviceCollection.AddSingleton<ISolutionManager, SolutionManager>();

        // Register view models
        serviceCollection.AddTransient<Chessboard>();
        serviceCollection.AddTransient<MainViewModel>();

        // Register views
        serviceCollection.AddTransient<MainView>();

        return serviceCollection.BuildServiceProvider();
    }
}
