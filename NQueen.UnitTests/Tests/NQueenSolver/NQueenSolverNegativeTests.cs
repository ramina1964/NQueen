using NQueen.NextGenKernel.Utils;

namespace NQueen.UnitTests.Tests.NQueenSolver;

public class NQueenSolverNegativeTests : IDisposable
{
    public NQueenSolverNegativeTests()
    {
        // Initialize the test-specific service provider
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();

        _serviceProvider = services.BuildServiceProvider();
        _solverBackEnd = _serviceProvider.GetRequiredService<ISolver>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldNotGenerateAnySolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = ExpectedSolutionData
            .SingleSolutions.GetValueOrDefault(boardSize)
            ?.Select(solution => new Solution(solution)).ToList()
            ?? [];

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsForBoardAsync(boardSize, solutionMode);

        // Assert
        Assert.Empty(actualSolutions.Solutions);
    }

    public void Dispose() => _serviceProvider.Dispose();

    private readonly ISolver _solverBackEnd;
    private readonly ServiceProvider _serviceProvider;
}
