namespace NQueen.Kernel.Solvers;

using NQueen.Domain.Context;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;

public class UniqueSolutionExamplesAndCountSolver
{
    private readonly ISolutionFormatter _formatter;
    private readonly int _exampleCap;

    public UniqueSolutionExamplesAndCountSolver(ISolutionFormatter formatter, int exampleCap = 5)
    {
        _formatter = formatter;
        _exampleCap = exampleCap;
    }

    public (SimulationResults examples, SimulationResults countOnly) Solve(SimulationContext context)
    {
        // Run a Unique mode solve materializing up to exampleCap solutions
        var exampleSolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, _exampleCap);
        exampleSolver.UseCountOnlyUniqueMode = false; // ensure full materialization (within cap)
        var examples = exampleSolver.Solve();

        // Run a Unique mode solve in count-only unique mode (returns representative sample capped at 0 => no enforced cap unless solver logic treats 0 as unlimited)
        // We set maxSolutionsInOutput = 0 to allow internal logic; flag drives count-only behavior.
        var countOnlySolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, 0);
        countOnlySolver.UseCountOnlyUniqueMode = true; // enable fast counting with symmetry weighting
        var countOnly = countOnlySolver.Solve();

        return (examples, countOnly);
    }
}
