namespace NQueen.UnitTests.Tests.NQueenSolverTests;

public class NegativeTests : IDisposable
{
    public NegativeTests()
    {
        // Initialize the test-specific service provider
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();
        
        _serviceProvider = services.BuildServiceProvider();

        // Resolve the ISolverBackEnd dependency
        _solverBackEnd = _serviceProvider.GetRequiredService<ISolverBackEnd>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldNotGenerateAnySolution(int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = ExpectedSolutionData.SingleSolutions.GetValueOrDefault(boardSize)
            ?? [];

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsAsync(boardSize, solutionMode);

        // Assert
        Assert.Empty(actualSolutions.Solutions);
    }

    public void Dispose()
    {
        // Dispose of the service provider to clean up resources
        _serviceProvider.Dispose();
    }
 
    private readonly ISolverBackEnd _solverBackEnd;
    private readonly ServiceProvider _serviceProvider;
}
