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

        // Register shared ViewModels
        services.AddNQueenViewModels();

        // Build and return the service provider
        return services.BuildServiceProvider();
    }
}