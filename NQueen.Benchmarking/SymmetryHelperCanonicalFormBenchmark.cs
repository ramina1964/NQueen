namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class SymmetryHelperCanonicalFormBenchmark
{
    [Params(8, 12, 16, 20)]
    public int BoardSize;

    [GlobalSetup]
    public void Setup()
    {
        solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++) solution[i] = i;
        scratch = new int[BoardSize * 8];
        resultBuffer = new int[BoardSize];
    }

    [Benchmark]
    public int[] CanonicalFormWithScratchAndBuffer()
    {
        return SymmetryHelper.GetCanonicalForm(solution, scratch, resultBuffer);
    }

    private int[] solution = System.Array.Empty<int>();
    private int[] scratch = System.Array.Empty<int>();
    private int[] resultBuffer = System.Array.Empty<int>();
}
