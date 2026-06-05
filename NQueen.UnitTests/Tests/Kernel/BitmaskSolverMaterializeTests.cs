namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Coverage-focused tests for <c>BitmaskSolver.Materialize.cs</c>.
/// Drives <c>SampleMaterializeUsingLookup</c>, <c>ConstructiveSampleSolutions</c>,
/// <c>GenerateConstructiveSolution</c> and <c>GenerateSymmetryVariants</c> through the
/// public <see cref="ISolverBackEnd.GetSimResultsAsync"/> API, exercising each path:
/// <list type="bullet">
///   <item>Lookup-materialize routing: <c>BoardSize &gt;= LookupThresholdN</c> (21) returns the
///   curated count and materialises sample solutions instead of enumerating.</item>
///   <item>Constructive sampling: <c>BoardSize &gt;= ConstructiveSampleThresholdN</c> (20) builds
///   samples from <c>GenerateConstructiveSolution</c> + symmetry variants (no DFS).</item>
///   <item>Both constructive special-case branches: N = 21 (n%6==3) and N = 26 (n%6==2).</item>
///   <item>Sample cap honoured (<c>MaxDisplayedCount</c> / explicit cap).</item>
///   <item>Every materialised sample is a conflict-free placement.</item>
///   <item>All and Unique modes both route through the materialize path.</item>
/// </list>
/// All sizes use the lookup/constructive path (no enumeration), so the suite stays fast.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "Materialize")]
public class BitmaskSolverMaterializeTests
{
    private static BitmaskSolver MakeSolver(int? maxDisplayedCount = null) =>
        maxDisplayedCount is int cap
            ? new(new SolutionFormatter(), cap) { EnableEvents = false }
            : new(new SolutionFormatter()) { EnableEvents = false };

    // -- Helper: validate that a row array is a legal placement ---------------

    private static void AssertValidPlacement(int[] rows)
    {
        int n = rows.Length;
        rows.Distinct().Should().HaveCount(n, "no two queens on the same row");
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                Math.Abs(rows[i] - rows[j]).Should().NotBe(j - i,
                    $"queens at columns {i} and {j} must not share a diagonal");
    }

    // -- All-mode lookup-materialize -----------------------------------------

    [Theory]
    [InlineData(21)] // n % 6 == 3 — constructive special-case branch
    [InlineData(26)] // n % 6 == 2 — constructive special-case branch
    public async Task AllMode_Materialize_LookupBoard_ReturnsCuratedCountWithValidSamples(int n)
    {
        // BoardSize >= LookupThresholdN (21): HandleModeCommon serves the count from the
        // lookup table and calls SampleMaterializeUsingLookup. Because N >= 20
        // (ConstructiveSampleThresholdN), samples come from ConstructiveSampleSolutions
        // (GenerateConstructiveSolution + GenerateSymmetryVariants) with no DFS.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n),
            $"All count for N={n} must equal the curated lookup value");
        result.Solutions.Should().NotBeEmpty("materialize path must surface sample solutions");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "samples must be capped by MaxDisplayedCount");

        foreach (var sol in result.Solutions)
            AssertValidPlacement(sol.QueenPositions);
    }

    // -- Unique-mode lookup-materialize --------------------------------------

    [Theory]
    [InlineData(21)] // n % 6 == 3
    [InlineData(26)] // n % 6 == 2
    public async Task UniqueMode_Materialize_LookupBoard_ReturnsCuratedCountWithValidSamples(int n)
    {
        // Same routing as All but with isUnique: true; ConstructiveSampleSolutions stores
        // every sample in _largeBoardRawSolutions (raw int[]), so the boards are surfaced
        // directly and must still be conflict-free.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(n),
            $"Unique count for N={n} must equal the curated lookup value");
        result.Solutions.Should().NotBeEmpty("materialize path must surface sample solutions");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "samples must be capped by MaxDisplayedCount");

        foreach (var sol in result.Solutions)
            AssertValidPlacement(sol.QueenPositions);
    }

    // -- Cap behaviour --------------------------------------------------------

    [Fact]
    public async Task Materialize_RespectsExplicitDisplayCap()
    {
        // A display cap of 3 must clamp the number of materialised samples regardless of how
        // many symmetry variants the constructive sampler could otherwise produce.
        const int cap = 3;
        using var solver = MakeSolver(maxDisplayedCount: cap);
        var ctx = new SimulationContext(21, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(21));
        result.Solutions.Should().NotBeEmpty();
        result.Solutions.Count.Should().BeLessThanOrEqualTo(cap, "explicit cap must be honoured");
        foreach (var sol in result.Solutions)
            AssertValidPlacement(sol.QueenPositions);
    }

    [Fact]
    public async Task Materialize_DistinctSamples_AreReturned()
    {
        // The constructive base plus its symmetry variants should yield more than one distinct
        // sample, exercising GenerateSymmetryVariants. Unique mode stores each sample as a raw
        // int[] (no canonical de-duplication), so the variant boards remain distinct — unlike
        // All mode, where samples are keyed by canonical form and the symmetry variants of a
        // single base collapse to one representative.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(21, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.Solutions.Count.Should().BeGreaterThan(1,
            "constructive base + symmetry variants should surface multiple samples");

        var distinct = result.Solutions
            .Select(s => string.Join(',', s.QueenPositions))
            .Distinct()
            .Count();
        distinct.Should().Be(result.Solutions.Count, "materialised samples should be distinct");
    }

    // -- Solver-state reset across consecutive runs --------------------------

    [Fact]
    public async Task Materialize_SameSolverInstance_ResetsBetweenRuns()
    {
        using var solver = MakeSolver();

        var first = await solver.GetSimResultsAsync(
            new SimulationContext(21, SolutionMode.All, DisplayMode.Hide));
        var second = await solver.GetSimResultsAsync(
            new SimulationContext(26, SolutionMode.All, DisplayMode.Hide));

        first.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(21));
        second.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(26),
            "a reused solver instance must reset sample state between runs");
        second.Solutions.Should().NotBeEmpty();
        foreach (var sol in second.Solutions)
            AssertValidPlacement(sol.QueenPositions);
    }
}
