namespace NQueen.Kernel.Solvers;

/// <summary>
/// Utility wrapper for running the Unique solver either (a) producing a small sample of example
/// unique solutions or (b) in pure count-only mode. Returns a tuple so callers can decide which
/// side to use depending on the requested mode.
/// </summary>
public class UniqueSolutionExamplesAndCountSolver
{
    private readonly ISolutionFormatter _formatter;
    private readonly int _exampleCap;

    public UniqueSolutionExamplesAndCountSolver(ISolutionFormatter formatter, int exampleCap = 5)
    {
        _formatter = formatter;
        _exampleCap = exampleCap;
    }

    /// <summary>
    /// Executes the Unique mode either returning materialized example solutions (left item)
    /// or a count-only result (right item). The unused side of the tuple is an empty results
    /// instance carrying only elapsed time for symmetry with the active mode.
    /// </summary>
    /// <param name="context">Simulation context (BoardSize, DisplayMode, etc.). SolutionMode is ignored and forced to Unique.</param>
    /// <param name="useCountOnlyUnique">If true, run count-only path; otherwise materialize examples up to the configured cap.</param>
    public (SimulationResults examples, SimulationResults countOnly) Solve(SimulationContext context, bool useCountOnlyUnique = false)
    {
        if (useCountOnlyUnique)
        {
            // Count-only: no materialized solutions retained => examples side is empty.
            using var countOnlySolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, maxSolutionsInOutput: 0)
            {
                UseCountOnlyUniqueMode = true,
                EnableEvents = false // no need for events in pure counting helper
            };
            var countOnly = countOnlySolver.Solve();
            return (new SimulationResults([], 0, countOnly.ElapsedTimeInSec), countOnly);
        }
        else
        {
            // Example mode: cap governs how many canonical unique solutions are materialized.
            using var exampleSolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, _exampleCap)
            {
                UseCountOnlyUniqueMode = false
            };
            var examples = exampleSolver.Solve();
            return (examples, new SimulationResults([], 0, examples.ElapsedTimeInSec));
        }
    }
}
