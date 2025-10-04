namespace NQueen.Kernel.Solvers;

using NQueen.Domain.Context;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;

public class MaterializingUniqueSolver : IUniqueSolutionStrategy
{
    private readonly ISolutionFormatter _formatter;

    public MaterializingUniqueSolver(ISolutionFormatter formatter)
    {
        _formatter = formatter;
    }

    public SimulationResults Solve(SimulationContext context, int exampleCap = 5)
    {
        var solver = new BitmaskSolver(context.BoardSize, context.SolutionMode, context.DisplayMode, _formatter);
        solver.EnableEvents = false;
        solver.UseCountOnlyUniqueMode = false;
        // Cap solutions for display
        // BitmaskSolver already supports capping via DI/config
        return solver.Solve();
    }
}
