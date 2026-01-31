namespace NQueen.Benchmarking;

[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class SymmetryPrunedUniqueCounterBenchmark
{
    [Params(15, 17, 20)]
    public int BoardSize { get; set; }

    [Benchmark(Baseline = true)]
    public ulong CountUniqueSymmetryPruned() =>
        SymmetryPrunedUniqueCounter.Count(BoardSize, 0, null);
}
