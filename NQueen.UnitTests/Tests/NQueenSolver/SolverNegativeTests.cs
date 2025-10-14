namespace NQueen.UnitTests.Tests.NQueenSolver;

using NQueen.UnitTests.Fixtures;

public class SolverNegativeTests : IClassFixture<SolverBackEndFixture>
{
    private readonly ISolverBackEnd _solver;

    public SolverNegativeTests(SolverBackEndFixture fixture)
    {
        _solver = fixture.Sut;
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldNotGenerateAnySolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Act
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var actualSolutions = await _solver.GetSimResultsAsync(simContext);

        // Assert
        Assert.Empty(actualSolutions.Solutions);
    }
}
