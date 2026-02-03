namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CountUniqueHalfBoardBenchmarks
{
    [Params(18, 19, 20, 21, 22)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only (parallel) half-board N=18-22")]
    public ulong Unique_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.EnableEvents = false;
        solver.UseParallel = true;
        solver.UseCountOnlyUniqueMode = true;
        solver.EnablePrefixMinimalityPruning = true;
        solver.EnablePartialReflectionPruning = true;

        solver.SetSimulationToken(Guid.NewGuid());
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException("Unexpected zero unique solutions.");
        return results.SolutionsCount;
    }

    private sealed class NoopFormatter : ISolutionFormatter
    {
        public string FormatSolutions(IReadOnlyList<Position> queenPositions, IndexingType indexingType, int boardSize) => string.Empty;
    }
}