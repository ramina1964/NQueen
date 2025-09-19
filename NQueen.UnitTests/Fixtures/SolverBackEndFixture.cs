namespace NQueen.UnitTests.Fixtures;

public class SolverBackEndFixture
{
    public ISolverBackEndPruning Sut { get; }

    public ServiceProvider ServiceProvider { get; }

    public SolverBackEndFixture()
    {
        var services = new ServiceCollection();

        // Register application and test services
        services.AddApplicationServices();
        services.AddTestServices();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Resolve the pruning backend
        Sut = ServiceProvider.GetRequiredService<ISolverBackEndPruning>();
    }
}
