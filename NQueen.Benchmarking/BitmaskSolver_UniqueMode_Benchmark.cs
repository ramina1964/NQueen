using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class BitmaskSolver_UniqueMode_Benchmark
{
    private readonly ISolutionFormatter _formatter = new SolutionFormatter();

    [Params(14)]
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