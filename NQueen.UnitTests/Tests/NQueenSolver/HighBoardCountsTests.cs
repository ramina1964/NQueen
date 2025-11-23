namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
public class HighBoardCountsTests
{
    private readonly ISolverBackEnd _solver;
    public HighBoardCountsTests(SolverBackEndFixture fixture) => _solver = fixture.Sut;

    private static readonly bool FullCoverage = Environment.GetEnvironmentVariable("FULL_HIGHBOARD_COVERAGE") == "true";
    private static readonly int[] FullBoardSet = { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
    private static readonly int[] ReducedBoardSet = { 20, 25, 29 };
    public static TheoryData<int> HighBoards => new(FullCoverage ? FullBoardSet : ReducedBoardSet);

    // Single board for materialization sampling
    private const int SampleBoard = 20;

    // Unified test: count-only (All & Unique) plus Single-mode verification
    [Theory]
    [MemberData(nameof(HighBoards))]
    [Trait("Category", "HighBoard")]
    public async Task CountOnly_AllUnique_AndSingle(int n)
    {
        // All count-only
        _solver.UseCountOnlyAllMode = true; _solver.UseCountOnlyUniqueMode = false;
        var allCtx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var allRes = await _solver.GetSimResultsAsync(allCtx);
        allRes.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        allRes.Solutions.Should().BeEmpty();

        // Unique count-only
        _solver.UseCountOnlyAllMode = false; _solver.UseCountOnlyUniqueMode = true;
        var uniqCtx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var uniqRes = await _solver.GetSimResultsAsync(uniqCtx);
        uniqRes.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(n));
        uniqRes.Solutions.Should().BeEmpty();

        // Single-mode (only verify for reduced or full set as part of same run)
        var singleCtx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var singleRes = await _solver.GetSimResultsAsync(singleCtx);
        singleRes.SolutionsCount.Should().Be(1UL);
        singleRes.Solutions.Should().ContainSingle();
    }

    // Combined materialization sampling for both All and Unique (only sample board)
    [Fact]
    [Trait("Category", "HighBoard")]
    public async Task MaterializeSamples_AllAndUnique_SampleBoard()
    {
        // All mode sample
        _solver.UseCountOnlyAllMode = false; _solver.UseCountOnlyUniqueMode = false;
        var allCtx = new SimulationContext(SampleBoard, SolutionMode.All, DisplayMode.Hide);
        var allRes = await _solver.GetSimResultsAsync(allCtx);
        allRes.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(SampleBoard));
        allRes.Solutions.Count.Should().BeGreaterThan(0);
        (allRes.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();

        // Unique mode sample
        _solver.UseCountOnlyAllMode = false; _solver.UseCountOnlyUniqueMode = false;
        var uniqCtx = new SimulationContext(SampleBoard, SolutionMode.Unique, DisplayMode.Hide);
        var uniqRes = await _solver.GetSimResultsAsync(uniqCtx);
        uniqRes.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(SampleBoard));
        uniqRes.Solutions.Count.Should().BeGreaterThan(0);
        (uniqRes.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
    }

    // Performance N=19 test only when explicitly requested (lookup shortcut, no enumeration)
    [Fact]
    [Trait("Category", "HighBoard")]
    [Trait("Category", "Perf")]
    public async Task UniqueMode_OptimizedEnumeration_N19()
    {
        if (Environment.GetEnvironmentVariable("PERF_N19") != "1")
            return;

        // Warmup small unique boards to JIT & prime caches (fast)
        foreach (var s in new[] { 12, 13 })
        {
            _solver.UseCountOnlyUniqueMode = true; _solver.UseCountOnlyAllMode = false;
            var warmCtx = new SimulationContext(s, SolutionMode.Unique, DisplayMode.Hide);
            var warmRes = await _solver.GetSimResultsAsync(warmCtx);
            warmRes.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(s));
        }

        // N=19 enumeration (symmetry-pruned path; threshold=20 so no lookup)
        _solver.UseCountOnlyUniqueMode = true; _solver.UseCountOnlyAllMode = false;
        var ctx = new SimulationContext(19, SolutionMode.Unique, DisplayMode.Hide);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var res = await _solver.GetSimResultsAsync(ctx);
        sw.Stop();
        res.Solutions.Should().BeEmpty(); // count-only mode
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetUnique(19));
        string fileName = "Unique_OptimizedEnumeration_N19.txt";
        string path = Path.Combine(Environment.CurrentDirectory, fileName);
        File.WriteAllLines(path, new[]
        {
            "OPTIMIZED SYMMETRY-PRUNED ENUMERATION N=19",
            "Env: PERF_N19=1",
            "LookupThreshold: 20 (enumeration used)",
            $"BoardSize: 19",
            $"UniqueCount: {res.SolutionsCount}",
            $"ReportedElapsedSeconds: {res.ElapsedTimeInSec:F3}",
            $"StopwatchElapsedSeconds: {sw.Elapsed.TotalSeconds:F3}",
            "MaterializedSolutions: 0 (count-only)",
            "Path: UniqueSolutionCounter.Count + symmetry pruning"
        });
    }

    // Heavy full enumeration test (disabled unless explicitly enabled)
    [Fact]
    [Trait("Category", "HighBoard")]
    [Trait("Category", "Heavy")]
    public void UniqueMode_FullEnumeration_N19()
    {
        if (Environment.GetEnvironmentVariable("RUN_UNIQUE19_ENUM") != "1") return; // heavy gating
        var sw = System.Diagnostics.Stopwatch.StartNew();
        ulong count = NQueen.Kernel.Solvers.Engines.CanonicalUniqueSearchEngine.CountUnique(19);
        sw.Stop();
        string fileName = "Unique_FullEnumeration_N19.txt";
        string path = Path.Combine(Environment.CurrentDirectory, fileName);
        File.WriteAllLines(path, new[]
        {
            "FULL UNIQUE ENUMERATION N=19",
            "Env: RUN_UNIQUE19_ENUM=1",
            $"ExpectedLookupCount: {ExpectedSolutionCounts.GetUnique(19)}",
            $"EnumeratedCount: {count}",
            $"ElapsedSeconds: {sw.Elapsed.TotalSeconds:F2}",
            $"ElapsedHHMMSS: {sw.Elapsed:hh\\:mm\\:ss}",
            "Note: This test performs exhaustive canonical minimality enumeration and can take a very long time."
        });
        count.Should().Be(ExpectedSolutionCounts.GetUnique(19));
    }
}
