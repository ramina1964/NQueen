namespace NQueen.Benchmarking;

public class SymmetryHelperCanonicalKeyBenchmark
{
    [Params(12, 14, 16, 18)]
    public int BoardSize;

    [GlobalSetup]
    public void Setup()
    {
        _solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++)
            _solution[i] = i;
        _scratch = new int[BoardSize * 2];
    }

    [Benchmark]
    public UInt128 GetCanonicalKey()
    {
        _lastKey = SymmetryHelper.GetCanonicalKey(_solution, _scratch, out _);

        // Guard to prevent JIT elision of the result.
        if (BoardSize > 8 && _lastKey == 0)
            throw new InvalidOperationException();
        return _lastKey;
    }

    private int[] _solution = null!;
    private int[] _scratch = null!;
    private UInt128 _lastKey;
}