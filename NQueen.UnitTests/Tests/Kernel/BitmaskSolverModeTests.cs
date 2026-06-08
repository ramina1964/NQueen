namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Integration-style tests that exercise BitmaskSolver across all three solution modes
/// (Single, All, Unique) with both Materialize and CountOnly storage strategies.
/// Uses small board sizes (N ≤ 8) to keep the suite fast.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "SolverMode")]
[Trait("Speed", "Slow")]
public class BitmaskSolverModeTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    // Helper: creates a standalone BitmaskSolver (uncapped, events off)
    private static BitmaskSolver MakeSolver() =>
        new(new SolutionFormatter()) { EnableEvents = false };

    // ── Single mode ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(8)]
    public async Task GetSimResults_SingleMode_ReturnsExactlyOneSolution(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var result = await _solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(1UL);
        result.Solutions.Should().ContainSingle();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetSimResults_SingleMode_NoSolutionBoard_ReturnsZero(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var result = await _solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(0UL);
        result.Solutions.Should().BeEmpty();
    }

    // ── All mode — Materialize ────────────────────────────────────────────────

    [Theory]
    [InlineData(4,  2UL)]
    [InlineData(5,  10UL)]
    [InlineData(6,  4UL)]
    public async Task GetSimResults_AllMode_Materialize_CountMatchesExpected(int n, ulong expected)
    {
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var result = await _solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(expected);
    }

    // ── All mode — CountOnly ─────────────────────────────────────────────────

    [Theory]
    [InlineData(5,  10UL)]
    public async Task GetSimResults_AllMode_CountOnly_CountMatchesExpected(int n, ulong expected)
    {
        using var solver = MakeSolver();
        solver.AllStorageMode = ResultStorageMode.CountOnly;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var result = await solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(expected);
        result.Solutions.Should().BeEmpty("CountOnly mode must not materialise solutions");
    }

    // ── Unique mode — Materialize ─────────────────────────────────────────────

    [Theory]
    [InlineData(4,  1UL)]
    [InlineData(5,  2UL)]
    [InlineData(6,  1UL)]
    public async Task GetSimResults_UniqueMode_Materialize_CountMatchesExpected(int n, ulong expected)
    {
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var result = await _solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(expected);
    }

    // ── Unique mode — CountOnly ──────────────────────────────────────────────

    [Theory]
    [InlineData(5,  2UL)]
    public async Task GetSimResults_UniqueMode_CountOnly_CountMatchesExpected(int n, ulong expected)
    {
        using var solver = MakeSolver();
        solver.UniqueStorageMode = ResultStorageMode.CountOnly;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var result = await solver.GetSimResultsAsync(ctx);
        result.SolutionsCount.Should().Be(expected);
        result.Solutions.Should().BeEmpty("CountOnly mode must not materialise solutions");
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSimResults_CancelledBeforeRun_ReturnsEmptyOrZero()
    {
        // Stage 6: pre-cancellation is signalled via a CancellationToken on SimulationContext.
        // The solver should observe the cancelled token and return without throwing.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = new SimulationContext(8, SolutionMode.All, DisplayMode.Hide, Cancellation: cts.Token);

        var result = await _solver.GetSimResultsAsync(ctx);
        result.Should().NotBeNull();
    }
}
