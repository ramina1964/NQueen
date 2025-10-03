namespace NQueen.UnitTests.Fixtures;

/// <summary>
/// Common fixture for solver backend tests: builds a provider with uncapped solver & test formatter.
/// </summary>
public class SolverBackEndFixture
{
    public SolverBackEndFixture()
    {
        var services = new ServiceCollection()
            .AddApplicationServices()
            .AddTestServices();

        ServiceProvider = services.BuildServiceProvider();
        Sut = ServiceProvider.GetRequiredService<ISolverBackEnd>();
    }

    public ISolverBackEnd Sut { get; }

    public ServiceProvider ServiceProvider { get; }
}
