using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;

namespace NQueen.Benchmarking;

// Measures impact of new parallel tunables (UseParallel + ParallelRootSplitDepth)
// Focuses on SolutionMode.All (root splitting currently applies only there).
// Returns total solutions count to keep work from being optimized away.
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class BitmaskSolverParallelScalingBenchmarks
{
    // Keep sizes moderate; higher sizes (>=18) may make runs very long with depth >1.
    [Params(10, 12, 14)]
    public int BoardSize;

    // Parallel on/off toggle (sequential acts as baseline cost of core search without task overhead).
    [Params(false, true)]
    public bool UseParallel;

    // Root split depth for parallel decomposition (ignored when UseParallel == false).
    // Depth 1 equals original implementation; depth 2/3 create more, smaller tasks (may help load balance for large N).
    [Params(1, 2, 3)]
    public int RootSplitDepth;

    // Enable adaptive split depth selection (if true, overrides RootSplitDepth with auto selection).
    [Params(false, true)]
    public bool UseAdaptiveDepth;

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true)]
    public ulong SolveAll()
    {
        int splitDepth = UseAdaptiveDepth ? -1 : RootSplitDepth;
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            DelayInMillisec = 0,
            UseParallel = UseParallel,
            ParallelRootSplitDepth = splitDepth
        };

        var results = solver.Solve();
        return results.SolutionsCount; // ensure work is not elided
    }
}
