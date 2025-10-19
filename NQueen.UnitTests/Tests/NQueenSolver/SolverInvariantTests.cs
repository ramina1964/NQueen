namespace NQueen.UnitTests.Tests.NQueenSolver;

/// <summary>
/// Solver invariants & edge cases: solution array shape and boards with no solutions.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "Enumeration")]
public class SolverInvariantTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SmallValueCases), MemberType = typeof(NQueenTestSets))]
    public async Task SolutionsHaveExpectedLength(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        foreach (var sol in results.Solutions)
        {
            var rows = sol.QueenPositions;
            rows.Should().NotBeNull();
            rows.Length.Should().Be(boardSize);
            rows.Length.Should().BeGreaterThan(0);
        }
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData), MemberType = typeof(NQueenTestSets))]
    public async Task BoardsWithoutSolutionsReturnEmptyList(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.Should().BeEmpty();
        results.SolutionsCount.Should().Be(0);
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
