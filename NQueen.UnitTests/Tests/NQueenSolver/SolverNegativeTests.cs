namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
public class SolverNegativeTests(SolverBackEndFixture fixture)
{
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

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
