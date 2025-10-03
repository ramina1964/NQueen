using BenchmarkDotNet.Attributes;
using NQueen.Domain.Utils;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[CPUUsageDiagnoser]
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
        foreach (var sol in _solutions)
        {
            var canonical = SymmetryHelper.GetCanonicalForm(sol);
            // Prevent JIT elision
            if (canonical[0] < -1) throw new System.InvalidOperationException();
        }
    }
}

[MemoryDiagnoser]
[CPUUsageDiagnoser]
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
        _scratch = new int[BoardSize];
    }

    [Benchmark]
    public void ColdInsertions()
    {
        var set = new HashSet<int[]>(new IntArrayComparer());
        foreach (var sol in _solutions)
        {
            SymmetryHelper.AddIfUnique(sol, set, _scratch);
        }
        if (set.Count < -1) throw new System.InvalidOperationException();
    }

    [Benchmark]
    public void DuplicateInsertions()
    {
        var set = new HashSet<int[]>(new IntArrayComparer());
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
