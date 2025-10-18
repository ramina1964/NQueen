using BenchmarkDotNet.Attributes;
using NQueen.Domain.Utils;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;
[CPUUsageDiagnoser]
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
        if (_solution == null || _scratch == null)
            throw new InvalidOperationException("Benchmark not initialized. Call Setup() first.");
        _lastKey = SymmetryHelper.GetCanonicalKey(_solution, _scratch, out _);

        // Guard logic to prevent JIT elision
        if (BoardSize > 8 && _lastKey == 0)
            throw new InvalidOperationException();
        return _lastKey;
    }

    private int[]? _solution;
    private int[]? _scratch;
    private UInt128 _lastKey;
}