namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class SymmetryPrunedUniqueCounterBenchmark
{
    [Params(15, 16, 17, 18, 20)]
    public int BoardSize { get; set; }

    [Benchmark(Baseline = true)]
    public ulong CountUniqueSymmetryPruned()
    {
        // No materialization, pure count
        return SymmetryPrunedUniqueCounter.Count(BoardSize, 0, null);
    }
}
