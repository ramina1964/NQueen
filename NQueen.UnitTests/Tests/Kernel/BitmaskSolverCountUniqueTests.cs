namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Coverage-focused tests for <c>BitmaskSolver.CountUnique.cs</c>.
/// Drives the private <c>CountUniqueAdaptive</c> / <c>CountUniqueFastHalfBoard</c>
/// methods through the public <see cref="ISolverBackEnd.GetSimResultsAsync"/> API
/// using <see cref="SolutionMode.Unique"/> + count-only storage so no solutions
/// are materialised. Uses small board sizes (N ≤ 9) for the parallel-canonical
/// branch and a single N = 16 case for the half-board branch — both fast.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "CountUnique")]
public class BitmaskSolverCountUniqueTests
{
    // Helper: standalone uncapped solver with events off.
    private static BitmaskSolver MakeSolver() =>
        new(new SolutionFormatter()) { EnableEvents = false };

    // ── Adaptive routing — small N (BitmaskParallelEngine.RunUnique branch) ─

    [Theory]
    [InlineData(1,  1UL)]
    [InlineData(2,  0UL)]
    [InlineData(3,  0UL)]
    [InlineData(4,  1UL)]
    [InlineData(5,  2UL)]
    [InlineData(6,  1UL)]
    [InlineData(7,  6UL)]
    [InlineData(8, 12UL)]
    [InlineData(9, 46UL)]
    public async Task CountUniqueAdaptive_SmallN_ParallelCanonicalBranch_MatchesExpected(int n, ulong expected)
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyUniqueMode = true;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(expected, $"unique count-only must equal expected for N={n}");
        result.Solutions.Should().BeEmpty("count-only must not materialise solutions");
    }

    [Fact]
    public async Task CountUniqueAdaptive_SmallN_AgreesWithLookupTable()
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyUniqueMode = true;

        for (int n = 1; n <= 9; n++)
        {
            var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
            var result = await solver.GetSimResultsAsync(ctx);
            result.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(n),
                $"adaptive count must match curated unique count for N={n}");
        }
    }

    // ── Pruning-flag save/restore semantics ─────────────────────────────────

    [Fact]
    public async Task CountUniqueAdaptive_PreservesPruningFlags_WhenInitiallyFalse()
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyUniqueMode = true;
        solver.EnablePrefixMinimalityPruning = false;
        solver.EnablePartialReflectionPruning = false;
        var ctx = new SimulationContext(6, SolutionMode.Unique, DisplayMode.Hide);

        await solver.GetSimResultsAsync(ctx);

        solver.EnablePrefixMinimalityPruning.Should().BeFalse(
            "CountUniqueAdaptive must restore EnablePrefixMinimalityPruning to its caller-supplied value");
        solver.EnablePartialReflectionPruning.Should().BeFalse(
            "CountUniqueAdaptive must restore EnablePartialReflectionPruning to its caller-supplied value");
    }

    [Fact]
    public async Task CountUniqueAdaptive_PreservesPruningFlags_WhenInitiallyTrue()
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyUniqueMode = true;
        solver.EnablePrefixMinimalityPruning = true;
        solver.EnablePartialReflectionPruning = true;
        var ctx = new SimulationContext(6, SolutionMode.Unique, DisplayMode.Hide);

        await solver.GetSimResultsAsync(ctx);

        solver.EnablePrefixMinimalityPruning.Should().BeTrue();
        solver.EnablePartialReflectionPruning.Should().BeTrue();
    }

    [Fact]
    public async Task CountUniqueAdaptive_HalfBoardBranch_PreservesPruningFlags()
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyUniqueMode = true;
        solver.EnablePrefixMinimalityPruning = false;
        solver.EnablePartialReflectionPruning = false;
        var ctx = new SimulationContext(16, SolutionMode.Unique, DisplayMode.Hide);

        await solver.GetSimResultsAsync(ctx);

        solver.EnablePrefixMinimalityPruning.Should().BeFalse(
            "CountUniqueFastHalfBoard must restore EnablePrefixMinimalityPruning after running");
        solver.EnablePartialReflectionPruning.Should().BeFalse(
            "CountUniqueFastHalfBoard must restore EnablePartialReflectionPruning after running");
    }

    // ── Storage-mode equivalence (UseCountOnlyUniqueMode vs UniqueStorageMode) ─

    [Fact]
    public async Task CountUniqueAdaptive_StorageModeCountOnly_RoutesThroughSamePath()
    {
        using var solver = MakeSolver();
        solver.UniqueStorageMode = ResultStorageMode.CountOnly;
        var ctx = new SimulationContext(7, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(6UL);
        result.Solutions.Should().BeEmpty();
    }
}
