namespace NQueen.UnitTests.Fixtures;

public class SolverBackEndFixture
{
    public ISolverBackEndPruning Sut { get; }
    public ServiceProvider ServiceProvider { get; }

    public SolverBackEndFixture()
    {
        var services = new ServiceCollection()
            .AddApplicationServices() // full solution set (uncapped)
            .AddTestServices();

        ServiceProvider = services.BuildServiceProvider();
        Sut = ServiceProvider.GetRequiredService<ISolverBackEndPruning>();
    }
}
