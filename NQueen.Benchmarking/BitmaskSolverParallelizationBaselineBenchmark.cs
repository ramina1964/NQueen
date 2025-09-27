using BenchmarkDotNet.Attributes;
using NQueen.KernelBitmask.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Models;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class BitmaskSolverParallelizationBaselineBenchmark
{
    [Params(10, 12, 14)]
    public int BoardSize;

    [Params(SolutionMode.All, SolutionMode.Unique, SolutionMode.Single)]
    public SolutionMode SolutionMode;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true)]
    public SimulationResults SolveSequential()
    {
        var solver = new BitmaskSolverExtended(BoardSize, SolutionMode, DisplayMode.Hide, _formatter)
        {
            DelayInMillisec = 0,
            EnableEvents = false,
            EnableParallelization = false
        };
        return solver.Solve();
    }

    [Benchmark]
    public SimulationResults SolveParallel()
    {
        var solver = new BitmaskSolverExtended(BoardSize, SolutionMode, DisplayMode.Hide, _formatter)
        {
            DelayInMillisec = 0,
            EnableEvents = false,
            EnableParallelization = true
        };
        return solver.Solve();
    }
}