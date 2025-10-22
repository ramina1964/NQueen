namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Enumeration")]
public class SolverAllModeIntegrityTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task AllMode_Parallel_SolutionsMaterializedHaveCorrectBoardSize(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().NotBeNull();
        foreach (var s in res.Solutions)
            s.BoardSize.Should().Be(n, $"Materialized solution should have BoardSize {n}");
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task AllMode_TotalCountMatchesExpected(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        var expected = ExpectedSolutionCounts.GetAll(n);
        res.SolutionsCount.Should().Be(expected, $"Total solution count mismatch for N={n}");
    }
}
