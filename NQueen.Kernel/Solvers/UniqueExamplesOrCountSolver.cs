namespace NQueen.Kernel.Solvers;

/// <summary>
/// Runs Unique-mode either materializing up to a sample cap of solutions or in count-only mode.
/// Returns a tuple where only one side is populated depending on requested path.
/// </summary>
public class UniqueExamplesOrCountSolver
{
    private readonly ISolutionFormatter _formatter;
    private readonly int _exampleCap;

    public UniqueExamplesOrCountSolver(ISolutionFormatter formatter, int exampleCap = 5)
    {
        _formatter = formatter;
        _exampleCap = exampleCap;
    }

    public (SimulationResults examples, SimulationResults countOnly) Solve(SimulationContext context, bool countOnly = false)
    {
        if (countOnly)
        {
            using var countOnlySolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, 0)
            {
                UseCountOnlyUniqueMode = true,
                EnableEvents = false
            };
            var countResults = countOnlySolver.Solve();
            return (new SimulationResults([], 0, countResults.ElapsedTimeInSec), countResults);
        }
        else
        {
            using var exampleSolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, _exampleCap)
            {
                UseCountOnlyUniqueMode = false
            };
            var examples = exampleSolver.Solve();
            return (examples, new SimulationResults([], 0, examples.ElapsedTimeInSec));
        }
    }
}
