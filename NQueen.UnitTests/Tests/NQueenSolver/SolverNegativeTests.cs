namespace NQueen.UnitTests.Tests.NQueenSolver;

public class SolverNegativeTests : IDisposable
{
    public SolverNegativeTests()
    {
        // Initialize the test-specific service provider
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();

        _serviceProvider = services.BuildServiceProvider();
        _solver = _serviceProvider.GetRequiredService<ISolverBackEnd>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldNotGenerateAnySolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Act
        var actualSolutions = await _solver
            .GetSimResultsAsync(new SimulationContext(boardSize, solutionMode, DisplayMode.Hide));

        // Assert
        Assert.Empty(actualSolutions.Solutions);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly ISolverBackEnd _solver;
    private readonly ServiceProvider _serviceProvider;
}
