namespace NQueen.Benchmarking;

public class SymmetryCanonicalFormBenchmark
{
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize;

    private List<int[]> _solutions = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate K valid solutions for BoardSize using All mode
        var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, new DefaultSolutionFormatter());
        var results = solver.Solve();
        // Take first 500 solutions (or all if fewer)
        _solutions = results.Solutions.Select(s => s.QueenPositions).Take(500).ToList();
    }

    [Benchmark]
    public void CanonicalizeAll()
    {
        var scratch = new int[BoardSize * 2];
        foreach (var sol in _solutions)
        {
            var key = SymmetryHelper.GetCanonicalKey(sol, scratch, out _);
            if (key == UInt128.MaxValue - 1) throw new System.InvalidOperationException();
        }
    }
}

public class SymmetryAddIfUniqueBenchmark
{
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize;

    private List<int[]> _solutions = null!;
    private int[] _scratch = null!;

    [GlobalSetup]
    public void Setup()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, new DefaultSolutionFormatter());
        var results = solver.Solve();
        _solutions = results.Solutions.Select(s => s.QueenPositions).Take(500).ToList();
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)]; // Use helper for consistency
    }

    [Benchmark]
    public void ColdInsertions()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        }
        if (set.Count < -1) throw new System.InvalidOperationException();
    }

    [Benchmark]
    public void DuplicateInsertions()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        }
        // Repeat insertions (should all be rejected)
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        }
        if (set.Count < -1) throw new System.InvalidOperationException();
    }
}
