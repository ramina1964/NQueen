namespace NQueen.Kernel.Solvers;

using NQueen.Domain.Context;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;

public class CountOnlyUniqueSolver : IUniqueSolutionStrategy
{
    private readonly ISolutionFormatter _formatter;

    public CountOnlyUniqueSolver(ISolutionFormatter formatter)
    {
        _formatter = formatter;
    }

    public SimulationResults Solve(SimulationContext context, int exampleCap = 0)
    {
        var solver = new BitmaskSolver(context.BoardSize, context.SolutionMode, context.DisplayMode, _formatter);
        solver.EnableEvents = false;
        solver.UseCountOnlyUniqueMode = true; // Count-only mode
        return solver.Solve();
    }
}
