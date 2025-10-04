namespace NQueen.Kernel.Solvers;

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
        // First, get example solutions (capped)
        var materializing = new MaterializingUniqueSolver(_formatter);
        var examples = materializing.Solve(context, _exampleCap);

        // Then, get the total count (count-only)
        var countOnlySolver = new CountOnlyUniqueSolver(_formatter);
        var countOnly = countOnlySolver.Solve(context, 0);

        return (examples, countOnly);
    }
}
