namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[CPUUsageDiagnoser]
public class NQueenBench
{
    [Params(20)]
    public int BoardSize { get; set; }

    [Benchmark]
    public long CountOnly() => BitboardNQueenSolver.CountSolutions(BoardSize, parallel: true);
}
