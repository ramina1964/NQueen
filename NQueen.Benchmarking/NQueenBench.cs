namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[CPUUsageDiagnoser]
public class NQueenBench
{
    [Params(20)]
    public int N { get; set; }

    [Benchmark]
    public long CountOnly() => BitboardNQueenSolver.CountSolutions(N, parallel: true);
}
