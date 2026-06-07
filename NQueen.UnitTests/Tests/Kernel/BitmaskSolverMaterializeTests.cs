namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Coverage-focused tests for <c>BitmaskSolver.Materialize.cs</c>.
/// Drives <c>SampleMaterializeUsingLookup</c> through the public
/// <see cref="ISolverBackEnd.GetSimResultsAsync"/> API, exercising each path:
/// <list type="bullet">
///   <item>Lookup-materialize routing: <c>BoardSize &gt;= LookupThresholdN</c> (21) returns the
///   curated count and materialises sample solutions instead of fully enumerating.</item>
///   <item>Early-exit DFS sampling: an early-exit DFS collects up to the display cap of
///   <em>genuinely distinct</em> solutions then stops (All mode via
///   <c>CollectAllSampleSolutionsDFS</c>, Unique mode via canonical <c>CollectUniqueSamplesDFS</c>).</item>
///   <item>Representative sizes: N = 21 (n%6==3) and N = 26 (n%6==2, &gt; 25 so raw-row storage).</item>
///   <item>Sample cap honoured (<c>MaxDisplayedCount</c> / explicit cap).</item>
///   <item>Every materialised sample is a conflict-free placement.</item>
///   <item>All and Unique modes both route through the materialize path.</item>
/// </list>
/// All sizes use the lookup count plus an early-exit DFS sample (no full enumeration), so the
/// suite stays fast.
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

    // -- Helper: canonical signature of a placement ---------------------------
    // Returns the lexicographically-minimal of the 8 symmetry transforms (rotations +
    // reflections) as a comma-joined string. Two boards that are rotations/reflections of one
    // another share the same canonical signature — so this is the correct way to test whether
    // two Unique samples are genuinely DIFFERENT fundamental solutions, not just different
    // orientations of the same one (the exact bug class fixed in the lookup-materialize path).
    // Uses GetCanonicalForm (sequence-based, size-agnostic) rather than the packed 5-bit key,
    // which is only valid for N <= 25.
    private static string CanonicalSignature(int[] rows)
    {
        var scratch = new int[rows.Length * 8];
        var canonical = SymmetryHelper.GetCanonicalForm(rows, scratch);
        return string.Join(',', canonical);
    }

    // -- All-mode lookup-materialize -----------------------------------------

    [Theory]
    [InlineData(21)] // n % 6 == 3 — constructive special-case branch
    [InlineData(26)] // n % 6 == 2 — constructive special-case branch
    public async Task AllMode_Materialize_LookupBoard_ReturnsCuratedCountWithValidSamples(int n)
    {
        // BoardSize >= LookupThresholdN (21): HandleModeCommon serves the count from the
        // lookup table and calls SampleMaterializeUsingLookup, which runs an early-exit DFS
        // (CollectAllSampleSolutionsDFS) that stops once the display cap of distinct samples
        // is reached — never a full enumeration.
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

        // Regression guard: the old constructive sampler collapsed All-mode samples to a single
        // canonical key (5 identical boards). The displayed boards must be distinct placements.
        result.Solutions
            .Select(s => string.Join(',', s.QueenPositions))
            .Distinct()
            .Count()
            .Should().Be(result.Solutions.Count, $"All-mode samples for N={n} must be distinct boards");
    }

    // -- Unique-mode lookup-materialize --------------------------------------

    [Theory]
    [InlineData(21)] // n % 6 == 3
    [InlineData(26)] // n % 6 == 2
    public async Task UniqueMode_Materialize_LookupBoard_ReturnsCuratedCountWithValidSamples(int n)
    {
        // Same routing as All but with isUnique: true; CollectUniqueSamplesDFS gathers up to
        // the cap of distinct *canonical* representatives. For N > 25 the samples are stored as
        // raw int[] (the packed key would be 0), so the boards are surfaced directly and must
        // still be conflict-free.
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

    // -- Unique-mode sample correctness (regression guard) -------------------

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(25)]
    public async Task UniqueMode_Materialize_SamplesAreCanonicalAndFundamentallyDistinct(int n)
    {
        // Regression guard for the lookup-materialize bug across the whole GUI Unique range
        // (N = 21..25, all >= LookupThresholdN). For each board the samples must be:
        //   (1) valid, conflict-free placements,
        //   (2) canonical representatives (IsIdentityCanonical), and
        //   (3) pairwise NOT symmetry-equivalent — i.e. genuinely different fundamental
        //       solutions, not rotations/reflections of one another.
        // The old constructive sampler violated (3) by emitting one base plus its symmetry
        // variants; this test would have caught that directly.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(n),
            $"Unique count for N={n} must equal the curated lookup value");
        result.Solutions.Should().NotBeEmpty("the materialize path must surface sample solutions");

        var scratch = new int[n * 8];
        foreach (var sol in result.Solutions)
        {
            AssertValidPlacement(sol.QueenPositions);
            SymmetryHelper.IsIdentityCanonical(sol.QueenPositions, scratch)
                .Should().BeTrue($"every Unique sample for N={n} must be its own canonical representative");
        }

        var canonicalSignatures = result.Solutions
            .Select(s => CanonicalSignature(s.QueenPositions))
            .ToList();
        canonicalSignatures.Distinct().Count().Should().Be(result.Solutions.Count,
            $"the {result.Solutions.Count} Unique samples for N={n} must be fundamentally distinct (no symmetry duplicates)");
    }

    // -- Cap behaviour --------------------------------------------------------

    [Fact]
    public async Task Materialize_RespectsExplicitDisplayCap()
    {
        // A display cap of 3 must clamp the number of materialised samples: the early-exit DFS
        // must stop after storing exactly `cap` distinct solutions.
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

    [Theory]
    [InlineData(SolutionMode.All)]
    [InlineData(SolutionMode.Unique)]
    public async Task Materialize_DistinctSamples_AreReturned(SolutionMode mode)
    {
        // The early-exit DFS sampler must surface several GENUINELY DISTINCT solutions for both
        // modes. This guards the regression where the old constructive sampler returned a single
        // base plus its symmetry variants: in All mode those variants collapsed to one canonical
        // representative (5 identical boards), and in Unique mode they were all orientations of a
        // single fundamental solution.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(21, mode, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.Solutions.Count.Should().BeGreaterThan(1,
            "the early-exit DFS should surface multiple samples");

        var distinct = result.Solutions
            .Select(s => string.Join(',', s.QueenPositions))
            .Distinct()
            .Count();
        distinct.Should().Be(result.Solutions.Count, "materialised samples should be distinct");

        if (mode == SolutionMode.Unique)
        {
            // Stronger guarantee for Unique mode: string-distinctness alone is NOT enough — two
            // boards can differ as strings yet still be rotations/reflections of the same
            // fundamental solution. Assert every sample is a canonical representative AND that
            // no two share a canonical signature, so each is a genuinely different fundamental
            // solution.
            var scratch = new int[21 * 8];
            foreach (var sol in result.Solutions)
                SymmetryHelper.IsIdentityCanonical(sol.QueenPositions, scratch)
                    .Should().BeTrue("each Unique sample must be its own canonical representative");

            var canonicalSignatures = result.Solutions
                .Select(s => CanonicalSignature(s.QueenPositions))
                .ToList();
            canonicalSignatures.Distinct().Count().Should().Be(result.Solutions.Count,
                "no two Unique samples may be symmetry-equivalent (rotations/reflections of one another)");
        }
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
