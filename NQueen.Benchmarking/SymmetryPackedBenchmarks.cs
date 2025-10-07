using BenchmarkDotNet.Attributes;
using NQueen.Domain.Models;
using NQueen.Domain.Utils;
using NQueen.Kernel.Solvers;
using System.Numerics;

namespace NQueen.Benchmarking;

// Compares legacy array-based uniqueness vs new packed-key (UInt128) uniqueness.
// Uses the same set of up to 500 solutions (from All mode) for fairness.
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class SymmetryAddIfUniquePackedComparisonBenchmark
{
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize;

    private List<int[]> _solutions = null!;
    private int[] _scratch = null!; // size 2N

    [GlobalSetup]
    public void Setup()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, new DefaultSolutionFormatter())
        {
            EnableEvents = false,
            UseParallel = true
        };
        var results = solver.Solve();
        _solutions = results.Solutions.Select(s => (int[])s.QueenPositions.Clone()).Take(500).ToList();
        _scratch = new int[SymmetryHelper.GetScratchBufferSize(BoardSize)];
    }

    // ---------------- Array-based (legacy) ----------------
    [Benchmark(Baseline = true)]
    public int ColdInsertions_ArraySet()
    {
        var set = new HashSet<int[]>(new IntArrayComparer());
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        }
        return set.Count;
    }

    [Benchmark]
    public int DuplicateInsertions_ArraySet()
    {
        var set = new HashSet<int[]>(new IntArrayComparer());
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        foreach (var sol in _solutions)
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        return set.Count;
    }

    // ---------------- Packed-key (new) ----------------
    [Benchmark]
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

    private sealed class IntArrayComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null || x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }
        public int GetHashCode(int[] obj)
        {
            unchecked
            {
                int h = 17;
                foreach (var v in obj) h = h * 31 + v;
                return h;
            }
        }
    }
}
