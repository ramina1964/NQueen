namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Enumeration")]
public class SolverInvariantTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SmallValueCases), MemberType = typeof(NQueenTestSets))]
    public async Task GetSimResults_ForAnyMode_SolutionsHaveExpectedLength(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        foreach (var sol in results.Solutions)
        {
            var rows = sol.QueenPositions;
            rows.ShouldNotBeNull();
            rows.Length.ShouldBe(boardSize);
            rows.Length.ShouldBeGreaterThan(0);
        }
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData), MemberType = typeof(NQueenTestSets))]
    public async Task GetSimResults_BoardsWithoutSolutions_ReturnsEmptyList(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.ShouldBeEmpty();
        results.SolutionsCount.ShouldBe(0UL);
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
