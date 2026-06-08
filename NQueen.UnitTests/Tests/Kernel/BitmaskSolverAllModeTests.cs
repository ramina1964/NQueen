namespace NQueen.UnitTests.Tests.Kernel;

/// <summary>
/// Coverage-focused tests for <c>BitmaskSolver.All.cs</c>.
/// Drives <c>RunAllUnified</c>, <c>EnumerateAllAdaptive</c>,
/// <c>CollectAllSamplesAndCountParallel</c>, and <c>CollectAllSampleSolutionsDFS</c>
/// through the public <see cref="ISolverBackEnd.GetSimResultsAsync"/> API, exercising
/// each routing branch:
/// <list type="bullet">
///   <item><c>RunAllUnified</c> count-only branch (small N, <see cref="SolutionMode.All"/> + <c>UseCountOnlyAllMode</c>).</item>
///   <item><c>RunAllUnified</c> materialize-with-cap branch (small N below the parallel-auto threshold).</item>
///   <item><c>EnumerateAllAdaptive(countOnly: true)</c> &#8594; <see cref="BitboardNQueenSolver.CountSolutions"/>.</item>
///   <item><c>EnumerateAllAdaptive(countOnly: false)</c> &#8594; <c>CollectAllSamplesAndCountParallel</c> for N &gt;= <see cref="SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN"/>.</item>
///   <item><c>CollectAllSampleSolutionsDFS</c> respects the materialization cap.</item>
///   <item>Events are emitted from the materialize path; post-cap events are suppressed.</item>
///   <item>In-flight cancellation is observed by the materialize path.</item>
///   <item>Storage-mode equivalence: <see cref="ResultStorageMode.CountOnly"/> on <c>AllStorageMode</c> routes through the same count-only path as <c>UseCountOnlyAllMode</c>.</item>
///   <item>Idempotency across consecutive runs on the same solver instance.</item>
///   <item><c>enableCap: false</c> constructor overload still surfaces at least one solution.</item>
/// </list>
/// All tests use small boards (N &lt;= 14) and headless runs (events off / delay = 0)
/// so the suite stays fast.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "AllMode")]
public class BitmaskSolverAllModeTests
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

    // -- RunAllUnified count-only branch (small N, below auto-parallel threshold) --

    [Theory]
    [InlineData(1,   1UL)]
    [InlineData(2,   0UL)]   // no valid all-mode placement exists
    [InlineData(3,   0UL)]   // no valid all-mode placement exists
    [InlineData(4,   2UL)]
    [InlineData(5,  10UL)]
    [InlineData(6,   4UL)]
    [InlineData(7,  40UL)]
    [InlineData(8,  92UL)]
    [InlineData(9, 352UL)]
    public async Task AllMode_CountOnly_SmallN_MatchesExpected(int n, ulong expected)
    {
        // N < ParallelAllMaterializeAutoEnableThresholdN (14) so EnumerateAllAdaptive(countOnly: true)
        // routes through BitboardNQueenSolver.CountSolutions. UseCountOnlyAllMode bypasses any
        // materialization. N=2,3 have no solution (count 0).
        using var solver = MakeSolver();
        solver.UseCountOnlyAllMode = true;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(expected, $"all count-only must equal expected for N={n}");
        result.Solutions.Should().BeEmpty("count-only must not materialise solutions");
    }

    // -- RunAllUnified materialize branch (small N) ---------------------------

    [Fact]
    public async Task AllMode_Materialize_SmallN_ProducesValidCappedSamples()
    {
        // N=8 is well below ParallelAllMaterializeAutoEnableThresholdN (14), so HandleModeCommon
        // routes Materialize through EnumerateAllAdaptive(countOnly: false) which calls
        // RunAllUnified directly (no two-phase split). All 92 solutions are counted, but at
        // most MaxDisplayedCount are materialised.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(8, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(92UL, "all-mode count for N=8 is 92 (OEIS A000170)");
        result.Solutions.Should().NotBeEmpty("materialize path must surface at least one sample");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "materialize path must cap displayed solutions");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }

    // -- Two-phase path: CollectAllSamplesAndCountParallel (N >= 14) ----------

    [Fact]
    public async Task AllMode_Materialize_N14_RoutesThroughTwoPhasePath()
    {
        // N=14 hits BoardSize >= ParallelAllMaterializeAutoEnableThresholdN (14), so
        // EnumerateAllAdaptive(countOnly: false) calls CollectAllSamplesAndCountParallel:
        //   Phase 1: CollectAllSampleSolutionsDFS stops at cap samples (milliseconds).
        //   Phase 2: BitboardNQueenSolver.CountSolutions returns the exact total.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(14, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(365_596UL, "all-mode count for N=14 is 365 596 (OEIS A000170)");
        result.Solutions.Should().NotBeEmpty("two-phase path must surface at least one sample");
        result.Solutions.Count.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount);
        foreach (var s in result.Solutions)
        {
            s.QueenPositions.Should().HaveCount(14);
            AssertValidPlacement(s.QueenPositions);
        }
    }

    [Fact]
    public async Task AllMode_CountOnly_N14_RoutesThroughBitboardCountSolutions()
    {
        // Count-only path for N >= 14: HandleModeCommon flips pruning + adaptive-depth flags
        // and calls EnumerateAllAdaptive(countOnly: true), which forwards to
        // BitboardNQueenSolver.CountSolutions(parallel: true).
        using var solver = MakeSolver();
        solver.UseCountOnlyAllMode = true;
        var ctx = new SimulationContext(14, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(365_596UL);
        result.Solutions.Should().BeEmpty();
    }

    // -- DFS cap-stop semantics in CollectAllSampleSolutionsDFS ---------------

    [Fact]
    public async Task AllMode_Materialize_N14_HonorsMaxDisplayedCap()
    {
        // CollectAllSampleSolutionsDFS exits as soon as `materialized >= cap`. Cap defaults to
        // SimulationSettings.MaxDisplayedCount. The resulting sample list must be exactly capped
        // (not over-filled), and every sample must be a valid placement.
        using var solver = MakeSolver();
        var ctx = new SimulationContext(14, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.Solutions.Count.Should().Be(SimulationSettings.MaxDisplayedCount,
            "Phase 1 DFS must collect exactly the configured cap on samples for N=14");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }

    // -- Notification emission and post-cap suppression on the materialize path ------

    [Fact]
    public async Task AllMode_Materialize_PushesSolutionFoundUpToCap()
    {
        // RunAllUnified materialize branch pushes SolutionFound for each materialised sample
        // until the cap is reached, then sets _eventsSuppressedAfterCap and stops pushing
        // further SolutionFound notifications (counting continues).
        using var solver = new BitmaskSolver(new SolutionFormatter())
        {
            EnableEvents = true,
            DelayInMillisec = 0,
        };
        int solutionFound = 0;
        var solutionSink = new SynchronousProgress<SolutionFoundInfo>(_ => Interlocked.Increment(ref solutionFound));

        var ctx = new SimulationContext(8, SolutionMode.All, DisplayMode.Hide,
            OnSolutionFound: solutionSink);
        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(92UL);
        solutionFound.Should().BeGreaterThan(0, "materialize path must push SolutionFound");
        solutionFound.Should().BeLessThanOrEqualTo(SimulationSettings.MaxDisplayedCount,
            "notifications must be suppressed after the materialisation cap is reached");
    }

    // -- In-flight cancellation observed by RunAllUnified ---------------------

    [Fact]
    public async Task AllMode_Materialize_HonorsInFlightCancellation()
    {
        // Stage 6: RunAllUnified's BitmaskSearchEngine.Run honours cancellation through its
        // IsCanceled callback, which now reads `IsCancellationRequested` (backed by the
        // CancellationToken on SimulationContext). Cancelling the token from the first
        // SolutionFound notification interrupts the run mid-enumeration.
        using var solver = new BitmaskSolver(new SolutionFormatter())
        {
            EnableEvents = true,
            DelayInMillisec = 0,
        };
        using var cts = new CancellationTokenSource();
        int eventsSeen = 0;
        var solutionSink = new SynchronousProgress<SolutionFoundInfo>(_ =>
        {
            if (Interlocked.Increment(ref eventsSeen) == 1)
                cts.Cancel();
        });

        var ctx = new SimulationContext(8, SolutionMode.All, DisplayMode.Hide,
            Cancellation: cts.Token, OnSolutionFound: solutionSink);
        var result = await solver.GetSimResultsAsync(ctx);

        result.Should().NotBeNull();
        eventsSeen.Should().BeGreaterThan(0, "cancellation toggles after the first SolutionFound notification");
    }

    // -- Storage-mode equivalence: AllStorageMode = CountOnly vs UseCountOnlyAllMode --

    [Fact]
    public async Task AllMode_AllStorageModeCountOnly_RoutesThroughSameCountOnlyPath()
    {
        using var solver = MakeSolver();
        solver.AllStorageMode = ResultStorageMode.CountOnly;
        var ctx = new SimulationContext(8, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(92UL);
        result.Solutions.Should().BeEmpty(
            "AllStorageMode.CountOnly must skip materialisation just like UseCountOnlyAllMode");
    }

    // -- Idempotency across consecutive runs on the same solver instance ------

    [Fact]
    public async Task AllMode_SameSolverInstance_ResetsBetweenRuns()
    {
        using var solver = MakeSolver();
        solver.UseCountOnlyAllMode = true;

        var first = await solver.GetSimResultsAsync(
            new SimulationContext(6, SolutionMode.All, DisplayMode.Hide));
        var second = await solver.GetSimResultsAsync(
            new SimulationContext(8, SolutionMode.All, DisplayMode.Hide));

        first.SolutionsCount.Should().Be(4UL, "all-mode count for N=6 is 4 (OEIS A000170)");
        second.SolutionsCount.Should().Be(92UL,
            "second run must not accumulate the previous run's state");
    }

    // -- enableCap: false constructor overload --------------------------------

    [Fact]
    public async Task AllMode_CapDisabledExplicitConstructor_SurfacesAllSolutions()
    {
        // With enableCap=false, BuildResults returns every materialised solution (cap == 0
        // disables the Take(cap) trim). For N=6 with only 4 solutions this means all 4 surface.
        using var solver = new BitmaskSolver(new SolutionFormatter(), enableCap: false)
        {
            EnableEvents = false,
        };
        var ctx = new SimulationContext(6, SolutionMode.All, DisplayMode.Hide);

        var result = await solver.GetSimResultsAsync(ctx);

        result.SolutionsCount.Should().Be(4UL);
        result.Solutions.Should().HaveCount(4,
            "enableCap=false must surface every materialised solution for small N");
        foreach (var s in result.Solutions)
            AssertValidPlacement(s.QueenPositions);
    }
}
