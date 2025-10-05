using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;

namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class UniqueCountingStrategiesBenchmark
{
    [Params(12, 13, 14, 15, 16)]
    public int BoardSize;

    [Params("Materialize", "CountOnly")]
    public string Strategy;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark]
    public SimulationResults SolveUnique()
    {
        BitmaskSolver solver;
        if (Strategy == "Materialize")
        {
            // Materialize up to 10 solutions
            solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter, 10);
            solver.UseCountOnlyUniqueMode = false;
        }
        else // CountOnly
        {
            // Count-only mode, no materialization
            solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter, 0);
            solver.UseCountOnlyUniqueMode = true;
        }
        solver.EnableEvents = false;
        return solver.Solve();
    }
}
