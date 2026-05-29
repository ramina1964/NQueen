namespace NQueen.Benchmarking;

/// <summary>Measures the cost of computing the canonical form of a solution array.</summary>
public class SymmetryCanonicalFormBenchmark
{
    [Params(8, 12, 16, 20)]
    public int BoardSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++) _solution[i] = i;
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)];
        _resultBuffer = new int[BoardSize];
    }

    [Benchmark]
    public int[] CanonicalForm() =>
        SymmetryHelper.GetCanonicalForm(_solution, _scratch, _resultBuffer);

    private int[] _solution = null!;
    private int[] _scratch = null!;
    private int[] _resultBuffer = null!;
}

/// <summary>Measures the cost of computing the packed canonical key of a solution array.</summary>
public class SymmetryCanonicalKeyBenchmark
{
    [Params(12, 14, 16, 18)]
    public int BoardSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _solution = new int[BoardSize];
        for (int i = 0; i < BoardSize; i++) _solution[i] = i;
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)];
    }

    [Benchmark]
    public UInt128 CanonicalKey()
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

/// <summary>Measures the cost of inserting solutions into a packed unique-solution set.</summary>
public class SymmetryAddIfUniquePackedBenchmark
{
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize { get; set; }

    private List<int[]> _solutions = null!;
    private int[] _scratch = null!;

    [GlobalSetup]
    public void Setup()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, new SolutionFormatter())
        {
            EnableEvents = false,
            UseParallel = true
        };
        var results = solver.Solve();
        _solutions = results.Solutions.Select(s => (int[])s.QueenPositions.Clone()).Take(500).ToList();
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)];
    }

    [Benchmark(Baseline = true)]
    public int ColdInsertions()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        return set.Count;
    }

    [Benchmark]
    public int DuplicateInsertions()
    {
        var set = new HashSet<UInt128>();
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUniquePacked(sol, set, _scratch, out _, out _);
        return set.Count;
    }
}

/// <summary>Measures the symmetry-pruned unique counter directly.</summary>
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class SymmetryPrunedUniqueCounterBenchmark
{
    [Params(15, 17, 20)]
    public int BoardSize { get; set; }

    [Benchmark(Baseline = true)]
    public ulong CountUniqueSymmetryPruned() =>
        SymmetryPrunedUniqueCounter.Count(BoardSize, 0, null);
}
