namespace NQueen.UnitTests.Fixtures;

public class SolverBackEndFixture
{
    public ISolverBackEnd Sut { get; }
    public ServiceProvider ServiceProvider { get; }

    public SolverBackEndFixture()
    {
        var services = new ServiceCollection()
            .AddApplicationServices()
            .AddTestServices();

        ServiceProvider = services.BuildServiceProvider();
        Sut = ServiceProvider.GetRequiredService<ISolverBackEnd>();
    }
}
