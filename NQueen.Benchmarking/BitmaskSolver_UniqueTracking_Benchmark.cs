using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Enums;
using NQueen.Domain.Settings;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;
[CPUUsageDiagnoser]
public class BitmaskSolver_UniqueTracking_Benchmark
{
    private readonly ISolutionFormatter _formatter = new SolutionFormatter();
    [Params(8, 10, 12, 14, 16)]
    public int BoardSize;
    [Benchmark]
    public SimulationResults SolveUnique()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.EnableEvents = false;
        solver.UseCountOnlyUniqueMode = false; // Materializing approach
        return solver.Solve();
    }
}