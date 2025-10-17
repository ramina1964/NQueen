using BenchmarkDotNet.Attributes;
using NQueen.Domain.Utils;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;
[CPUUsageDiagnoser]
public class SymmetryHelperCanonicalKeyBenchmark
{
    [Params(12, 14, 16, 18)]
    public int BoardSize;
    private int[] solution;
    private int[] scratch;
    private UInt128 lastKey;
    [GlobalSetup]
    public void Setup()
    {
        solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++)
            solution[i] = i;
        scratch = new int[BoardSize * 2];
    }

    [Benchmark]
    public UInt128 GetCanonicalKey()
    {
        lastKey = SymmetryHelper.GetCanonicalKey(solution, scratch, out var canonical);
        // Guard logic to prevent JIT elision
        if (BoardSize > 8 && lastKey == 0)
            throw new InvalidOperationException();
        return lastKey;
    }
}