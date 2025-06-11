namespace NQueen.ViewModelTests.Setup;

public static class TestServiceCollectionExtensions
{
    public static ServiceProvider InitializeForTests()
    {
        var services = new ServiceCollection();

        // Override IDispatcher with TestDispatcher for tests
        services.AddTransient<IDispatcher, TestDispatcher>();
        services.AddTransient<ISaveFileDialogService, MockSaveFileDialogService>();

        // Shared NQueen-Related Services
        services.AddNextGenNQueenServices();

        // Register specific services, i.e., views and view models
        services.AddTransient<ChessboardViewModel>();

        services.AddTransient<MainViewModel>();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}