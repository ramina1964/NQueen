namespace NQueen.UnitTests.Tests.NQueenSolver.Slow;

/// <summary>
/// Consolidated slow solution count verification tests for larger boards (>=9).
/// - Single mode: ensure exactly one solution materialized.
/// - Unique / All modes (count-only): verify total counts against SolutionCounts without materialization overhead.
/// Uses solver backend count-only flags to suppress solution storage for performance.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "Slow")]
public class SolverSolutionCountSlowTests(SolverBackEndFixture fixture)
{
    // Single mode larger boards (enumeration still feasible but separated from fast suite)
    [Theory]
    [MemberData(nameof(SingleBoards))]
    public async Task SingleMode_LargeBoards_ExactlyOneSolution(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.SolutionsCount.Should().Be(1UL, $"Single mode should yield exactly one solution for N={n}");
        results.Solutions.Should().ContainSingle();
    }

    // Unique count-only large boards
    [Theory]
    [MemberData(nameof(UniqueBoards))]
    public async Task UniqueMode_CountOnly_LargeBoards_TotalMatches(int n)
    {
        // Enable count-only to avoid storing solutions
        _solver.UseCountOnlyUniqueMode = true;
        _solver.UseCountOnlyAllMode = false;
        var expected = ExpectedSolutionCounts.GetUnique(n);
        expected.Should().BeGreaterThan(0UL, "Expected unique count must be positive.");
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty("Count-only mode should not materialize unique solutions.");
        res.SolutionsCount.Should().Be(expected, $"Unique count mismatch for N={n}");
    }

    // All count-only large boards
    [Theory]
    [MemberData(nameof(AllBoards))]
    public async Task AllMode_CountOnly_LargeBoards_TotalMatches(int n)
    {
        _solver.UseCountOnlyAllMode = true;
        _solver.UseCountOnlyUniqueMode = false;
        var expected = ExpectedSolutionCounts.GetAll(n);
        expected.Should().BeGreaterThan(0UL, "Expected all count must be positive.");
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty("Count-only mode should not materialize all solutions.");
        res.SolutionsCount.Should().Be(expected, $"All count mismatch for N={n}");
    }

    public static TheoryData<int> SingleBoards => [9, 10, 11, 12, 13];

    public static TheoryData<int> UniqueBoards => [9, 10, 11, 12, 13, 14];

    public static TheoryData<int> AllBoards => [9, 10, 11, 12, 13, 14];

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
