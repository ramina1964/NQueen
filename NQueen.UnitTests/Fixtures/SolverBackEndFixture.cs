namespace NQueen.UnitTests.Fixtures;

// Todo: The test methods are repetitive and hardcoded with inline data, making them brittle and harder to maintain.
// 1) Use DI of the ServiceCollection to make the fixture more flexible.
// 2) Refactor repetitive assertions into helper methods.

public class SolverBackEndFixture : IClassFixture<SolverBackEndFixture>
{
    public ISolverBackEnd Sut { get; }

    public ServiceProvider ServiceProvider { get; }

    public SolverBackEndFixture()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddNQueenServices();
        services.AddScoped<ISolverBackEnd, BackTrackingSolver>();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Resolve the ISolverBackEnd instance
        Sut = ServiceProvider.GetRequiredService<ISolverBackEnd>();
    }
}
