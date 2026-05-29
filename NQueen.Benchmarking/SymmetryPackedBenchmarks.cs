namespace NQueen.Benchmarking;

public class SymmetryAddIfUniquePackedBenchmark
{
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize;

    private List<int[]> _solutions = null!;
    private int[] _scratch = null!;

    [GlobalSetup]
    public void Setup()
    {
        var formatter = new SolutionFormatter();
        var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, formatter)
        {
            EnableEvents = false,
            UseParallel = true
        };
        var results = solver.Solve();
        _solutions = results.Solutions.Select(s => (int[])s.QueenPositions.Clone()).Take(500).ToList();
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)];
    }

    [Benchmark(Baseline = true)]
    public int ColdInsertions_PackedSet()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        }
        return set.Count;
    }

    [Benchmark]
    public int DuplicateInsertions_PackedSet()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        return set.Count;
    }
}
