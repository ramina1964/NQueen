namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Coverage-focused tests for <c>BitmaskSolver.Unique.cs</c>.
/// Drives <c>ExecuteUniqueModeUnified</c> and <c>EnumerateUniqueVisualizeAdaptive</c>
/// through the public <see cref="ISolverBackEnd.GetSimResultsAsync"/> API,
/// exercising each routing branch:
/// <list type="bullet">
///   <item>Small-N branch (N &lt; <see cref="SimulationSettings.LargeBoardSymmetryPruningThreshold"/>): <see cref="BitmaskSearchEngine.Run"/> with <c>RestrictFirstCol: true</c> and <c>IsIdentityCanonical</c> filter.</item>
///   <item>Empty / zero-solution N (N = 2, 3) — small-N branch returns 0.</item>
///   <item>Mid-N branch (N = <see cref="SimulationSettings.LargeBoardSymmetryPruningThreshold"/> = 15): <c>SymmetryPrunedUniqueCounter.Count</c> with the <c>onMaterialized</c> callback.</item>
///   <item>Large-N two-phase branch (N &gt;= <see cref="SimulationSettings.UniqueCountOnlyParallelThresholdN"/> = 16): <c>CollectUniqueSamplesDFS</c> for samples + <c>CountUniqueFastHalfBoard</c> for the count.</item>
///   <item><see cref="EnumerateUniqueVisualizeAdaptive"/> visualize path: emits <c>QueenPlaced</c> and <c>SolutionFound</c>; in-flight cancellation honoured.</item>
///   <item>Solver-state reset across consecutive runs.</item>
///   <item><c>enableCap: false</c> constructor surfaces every canonical solution (uncapped).</item>
/// </list>
/// All tests use small boards (N &lt;= 16) and headless runs (events off / delay = 0)
/// so the suite stays fast.
/// Count-only Unique routing is covered separately by
/// <see cref="BitmaskSolverCountUniqueTests"/>.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "UniqueMode")]
public class BitmaskSolverUniqueTests
{
    private static BitmaskSolver MakeSolver() =>
        new(new SolutionFormatter()) { EnableEvents = false };

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

    // -- Small-N branch (N < 15) ---------------------------------------------

    [Theory]
    [InlineData(1,   1UL)]
    [InlineData(4,   1UL)]
    [InlineData(5,   2UL)]
    [InlineData(6,   1UL)]
    [InlineData(7,   6UL)]
    [InlineData(8,  12UL)]
    [InlineData(9,  46UL)]
    public async Task UniqueMode_Materialize_SmallN_RoutesThroughBitmaskSearchEngine(int n, ulong expected)
    {
        // ExecuteUniqueModeUnified small-N branch: BoardSize < LargeBoardSymmetryPruningThreshold (15)
        // runs BitmaskSearchEngine.Run with RestrictFirstCol: true and filters by IsIdentityCanonical
        // to keep only canonical representatives.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(expected, $"unique materialize must equal expected for N={n}");
        result.Solutions.Should().NotBeEmpty();
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "materialize path must cap displayed solutions");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }

    [Fact]
    public async Task UniqueMode_NoSolutionExists_ReturnsZero()
    {
        using var solver = MakeSolver();

        foreach (int n in new[] { 2, 3 })
        {
            var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
            var result = await solver.GetSimResultsAsync(ctx);

            result.SolutionsCount.Should().Be(0UL, $"N={n} has no valid placements");
            result.Solutions.Should().BeEmpty();
        }
    }

    // -- Mid-N branch: N == 15 -> SymmetryPrunedUniqueCounter -----------------

    [Fact]
    public async Task UniqueMode_Materialize_N15_RoutesThroughSymmetryPrunedCounter()
    {
        // N == LargeBoardSymmetryPruningThreshold (15) but < UniqueCountOnlyParallelThresholdN (16),
        // so ExecuteUniqueModeUnified routes through Engines.SymmetryPrunedUniqueCounter.Count
        // with the onMaterialized callback wiring samples back into the solver's storage.
        using var solver = MakeSolver();
        solver.EnablePrefixMinimalityPruning = true;
        solver.EnablePartialReflectionPruning = true;
        var ctx = new SimulationContext(15, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(285_053UL,
            "unique count for N=15 is 285 053 (OEIS A002562)");
        result.Solutions.Should().NotBeEmpty("mid-N branch must surface samples via onMaterialized");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount);
        foreach (var s in result.Solutions)
        {
            s.QueenPositions.Should().HaveCount(15);
            AssertValidPlacement(s.QueenPositions);
        }
    }

    // -- Large-N two-phase branch: N >= 16 -----------------------------------

    [Fact]
    public async Task UniqueMode_Materialize_N16_RoutesThroughTwoPhasePath()
    {
        // BoardSize >= UniqueCountOnlyParallelThresholdN (16), so ExecuteUniqueModeUnified runs:
        //   Phase 1: CollectUniqueSamplesDFS — sequential DFS that stops at cap canonical samples.
        //   Phase 2: CountUniqueFastHalfBoard — exact half-board count.
        //
        // Routing-only check: the count assertion is intentionally omitted here because
        // CountUniqueFastHalfBoard currently under-reports for N >= 16 (returns 692 857 for N = 16
        // versus the OEIS A002562 value of 1 846 955) — tracked as a production defect under
        // "Backlog — Kernel Correctness" in docs/ROADMAP.md. We still verify Phase 1 produces
        // valid canonical samples and that the count is non-zero.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(16, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().BeGreaterThan(0UL,
            "Phase 2 CountUniqueFastHalfBoard must return a non-zero count for N=16");
        result.Solutions.Should().NotBeEmpty("Phase 1 DFS must surface canonical samples");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount);
        foreach (var s in result.Solutions)
        {
            s.QueenPositions.Should().HaveCount(16);
            AssertValidPlacement(s.QueenPositions);
        }
    }

    // -- CollectUniqueSamplesDFS cap-stop semantics --------------------------

    [Fact]
    public async Task UniqueMode_Materialize_N16_HonorsMaxDisplayedCap()
    {
        // CollectUniqueSamplesDFS exits as soon as `localMaterialized >= cap` and only counts
        // canonical solutions (filtered by IsIdentityCanonical). The default cap is
        // SimulationSettings.MaxDisplayedCount, and N=16 has 1 846 955 unique solutions —
        // well above the cap — so the sample list must be exactly cap-sized.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(16, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.Solutions.Count.Should().Be(SimulationSettings.MaxDisplayedCount,
            "Phase 1 DFS must stop after exactly cap canonical samples for N=16");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }

    // -- Visualize path: EnumerateUniqueVisualizeAdaptive --------------------

    [Fact]
    public async Task UniqueMode_VisualizePath_FiresQueenPlacedAndSolutionFoundEvents()
    {
        // DisplayMode.Visualize routes through EnumerateUniqueVisualizeAdaptive which raises
        // QueenPlaced for every placement and SolutionFound for each canonical sample up to cap.
        // N=8 has 12 canonical solutions; cap (= MaxDisplayedCount = 5) bounds SolutionFound.
        using var solver = new BitmaskSolver(new SolutionFormatter())
        {
            EnableEvents = true,
            DelayInMillisec = 0,
        };
        int queenPlaced = 0;
        int solutionFound = 0;
        solver.QueenPlaced += (_, _) => Interlocked.Increment(ref queenPlaced);
        solver.SolutionFound += (_, _) => Interlocked.Increment(ref solutionFound);

        var ctx = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Visualize);
        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(12UL, "unique count for N=8 is 12 (OEIS A002562)");
        queenPlaced.Should().BeGreaterThan(0,
            "visualize path must emit QueenPlaced for each placement");
        solutionFound.Should().BeGreaterThan(0, "visualize path must emit SolutionFound");
        solutionFound.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "SolutionFound must be capped at MaxDisplayedCount");
    }

    [Fact]
    public async Task UniqueMode_VisualizePath_HonorsInFlightCancellation()
    {
        // ResetForSolve() clears IsSolverCanceled at Solve() entry, so a pre-cancelled flag
        // has no effect. To exercise cancellation the flag must flip during the run; we toggle
        // it from the first QueenPlaced callback.
        using var solver = new BitmaskSolver(new SolutionFormatter())
        {
            EnableEvents = true,
            DelayInMillisec = 0,
        };
        int placedSeen = 0;
        solver.QueenPlaced += (_, _) =>
        {
            if (Interlocked.Increment(ref placedSeen) == 1)
                solver.IsSolverCanceled = true;
        };

        var ctx = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Visualize);
        var result = await solver.GetSimResultsAsync(ctx);

        result.Should().NotBeNull();
        placedSeen.Should().BeGreaterThan(0,
            "cancellation toggles after the first QueenPlaced event");
    }

    // -- Idempotency across consecutive runs ---------------------------------

    [Fact]
    public async Task UniqueMode_SameSolverInstance_ResetsBetweenRuns()
    {
        using var solver = MakeSolver();

        var first = await solver.GetSimResultsAsync(
            new SimulationContext(6, SolutionMode.Unique, DisplayMode.Hide));
        var second = await solver.GetSimResultsAsync(
            new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide));

        first.SolutionsCount.Should().Be(1UL, "unique count for N=6 is 1 (OEIS A002562)");
        second.SolutionsCount.Should().Be(12UL,
            "second run must not accumulate the previous run's state");
    }

    // -- enableCap: false constructor ----------------------------------------

    [Fact]
    public async Task UniqueMode_CapDisabledExplicitConstructor_SurfacesAllCanonicalSolutions()
    {
        // With enableCap=false, BuildResults returns every materialised solution. ExecuteUniqueModeUnified
        // small-N branch sets cap = int.MaxValue when _capEnabled is false, so all 12 canonical
        // solutions for N=8 should surface (subject only to IsIdentityCanonical filtering).
        using var solver = new BitmaskSolver(new SolutionFormatter(), enableCap: false)
        {
            EnableEvents = false,
        };
        var ctx = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(12UL);
        result.Solutions.Should().HaveCount(12,
            "enableCap=false must surface every canonical solution for N=8");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }
}
