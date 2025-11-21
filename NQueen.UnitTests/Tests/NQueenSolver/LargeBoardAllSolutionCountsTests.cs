namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category","LargeBoardAllCounts")]
[Trait("Category","Slow")]
public class LargeBoardAllSolutionCountsTests(SolverBackEndFixture fixture)
{
    // Boards starting at intermediate large size up to throttle threshold - 1 (to force enumeration path for All mode)
    public static TheoryData<int> LargeBoardsEnumerated =>
    [
        // 15 is below throttle threshold (16) and below lookup threshold (20) so enumeration must be correct.
        15, 16, 17
    ];

    // Verify All-mode counts (count-only) match expected lookup table values for enumerated large boards.
    [Theory]
    [MemberData(nameof(LargeBoardsEnumerated))]
    [Trait("Category","Slow")]
    public async Task AllMode_CountOnly_LargeBoards_Exact(int n)
    {
        _solver.UseCountOnlyAllMode = true; _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n), $"All-mode count mismatch for N={n}");
    }

    // Materialization sanity: ensure at least one solution materialized and count matches expected for sample board (16)
    [Fact]
    [Trait("Category","Slow")]
    public async Task AllMode_Materialize_SampleBoard16()
    {
        int n = 16;
        _solver.UseCountOnlyAllMode = false; _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        res.Solutions.Count.Should().BeGreaterThan(0);
        (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
