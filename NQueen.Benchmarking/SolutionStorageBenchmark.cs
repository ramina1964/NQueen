namespace NQueen.Benchmarking;

public class SolutionStorageBenchmark
{
    // Choose moderate sizes to keep runtime acceptable yet produce many solutions.
    // N=10 produces 724 (All) / 92 (Unique) solutions; N=12 produces 14200 (All) / 14200 (Unique?) depending on symmetry.
    // Keep to a single value for stable comparison; can extend later.
    [Params(10)]
    public int BoardSize;
    [Params(SolutionMode.All, SolutionMode.Unique)]
    public SolutionMode Mode;
    private ISolutionFormatter _formatter = new SolutionFormatter();
    [Benchmark]
    public SimulationResults EnumerateSolutions()
    {
        // Disable cap to force storage of all solutions (worst-case allocations baseline).
        var solver = new BitmaskSolver(BoardSize, Mode, DisplayMode.Hide, _formatter, maxSolutionsInOutput: int.MaxValue)
        {
            UseCountOnlyAllMode = false,
            UseCountOnlyUniqueMode = false
        };
        // NOTE: BitmaskSolver ctor with maxSolutionsInOutput:int.MaxValue combined with enableCap=true means no effective cap.
        // Solve and return results (BenchmarkDotNet will measure allocations for SimulationResults + internal arrays).
        return solver.Solve();
    }
}