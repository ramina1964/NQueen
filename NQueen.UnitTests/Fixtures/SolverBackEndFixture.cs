namespace NQueen.UnitTests.Fixtures;

/// <summary>
/// Common fixture for solver backend tests: builds a provider with uncapped solver & test formatter.
/// Adds disposal to ensure service provider resources released and future side-effects minimized.
/// </summary>
public class SolverBackEndFixture : IDisposable
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

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
