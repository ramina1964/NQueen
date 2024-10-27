namespace NQueen.UnitTests.Fixtures;

public class SolverBackEndFixture : IClassFixture<SolverBackEndFixture>
{
    public ISolverBackEnd Sut { get; }
    public ServiceProvider ServiceProvider { get; }

    public SolverBackEndFixture()
    {
        var services = new ServiceCollection();

        // Register shared services
        services.AddNQueenServices();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Resolve the ISolverBackEnd instance
        Sut = ServiceProvider.GetRequiredService<ISolverBackEnd>();
    }
}
