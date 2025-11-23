namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class SymmetryHelperCanonicalFormBenchmark
{
    [Params(8, 12, 16, 20)]
    public int BoardSize;

    [GlobalSetup]
    public void Setup()
    {
        _solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++) _solution[i] = i;
        _scratch = new int[BoardSize * 8];
        _resultBuffer = new int[BoardSize];
    }

    [Benchmark]
    public int[] CanonicalFormWithScratchAndBuffer() =>
        SymmetryHelper.GetCanonicalForm(_solution, _scratch, _resultBuffer);

    private int[] _solution = [];
    private int[] _scratch = [];
    private int[] _resultBuffer = [];
}
