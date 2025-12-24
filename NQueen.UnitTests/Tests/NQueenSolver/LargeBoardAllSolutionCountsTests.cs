namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "LargeBoardAllCounts")]
[Trait("Category", "Slow")]
public class LargeBoardAllSolutionCountsTests(SolverBackEndFixture fixture)
{
    private static readonly bool _enableFullAllEnum =
        Environment.GetEnvironmentVariable(NQueen.TestShared.TestSettings.EnvEnableFullAllEnum) == "1";

    // Permanently reduce dataset to avoid long-running enumerations in unit tests
    public static TheoryData<int> LargeBoardsEnumerated =>
    [
        .. (_enableFullAllEnum ? new[] { 15, 16, 17 } : new[] { 15 })
    ];

    // Verify All-mode counts (count-only) match expected lookup table values for enumerated large boards.
    [Theory]
    [MemberData(nameof(LargeBoardsEnumerated))]
    [Trait("Category", "Slow")]
    [Trait("SkipInCI", "true")]
    public async Task AllMode_CountOnly_LargeBoards_Exact(int n)
    {
        _solver.UseCountOnlyAllMode = true; _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n), $"All-mode count mismatch for N={n}");
    }

    // Materialization sanity: ensure at least one solution materialized and count matches expected for sample board (lookup path where possible)
    [Fact]
    [Trait("Category", "Slow")]
    [Trait("SkipInCI", "true")]
    public async Task AllMode_Materialize_SampleBoard()
    {
        // Prefer a board that uses the lookup path to avoid long enumeration in unit tests
        int n = NQueen.Domain.Settings.SimulationSettings.LookupThresholdN;
        _solver.UseCountOnlyAllMode = false; _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        res.Solutions.Count.Should().BeGreaterThan(0);
        (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
