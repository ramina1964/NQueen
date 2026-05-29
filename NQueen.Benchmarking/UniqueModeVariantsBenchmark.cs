namespace NQueen.Benchmarking;

/// <summary>
/// Compares Unique mode count-only vs materialise across the fast half-board region (N=16-20).
/// </summary>
public class UniqueModeVariantsBenchmark
{
    // Extended range to cover the full CountUniqueFastHalfBoard region (N=16..20).
    [Params(16, 17, 18, 19, 20)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new SolutionFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only (parallel)")]
    public ulong Unique_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true,
            UseParallel = true
        };
        return solver.Solve().SolutionsCount;
    }

    [Benchmark(Description = "Unique Materialize (sample, capped)")]
    public (int materialized, ulong total) Unique_Materialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = false,
            UseParallel = true
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}
