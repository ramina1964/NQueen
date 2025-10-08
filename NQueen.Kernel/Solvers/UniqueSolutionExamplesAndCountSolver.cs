namespace NQueen.Kernel.Solvers;

public class UniqueSolutionExamplesAndCountSolver
{
    private readonly ISolutionFormatter _formatter;
    private readonly int _exampleCap;

    public UniqueSolutionExamplesAndCountSolver(ISolutionFormatter formatter, int exampleCap = 5)
    {
        _formatter = formatter;
        _exampleCap = exampleCap;
    }

    public (SimulationResults examples, SimulationResults countOnly) Solve(SimulationContext context, bool useCountOnlyUnique = false)
    {
        if (useCountOnlyUnique)
        {
            var countOnlySolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, 0);
            countOnlySolver.UseCountOnlyUniqueMode = true;
            var countOnly = countOnlySolver.Solve();
            return (new SimulationResults([], 0, countOnly.ElapsedTimeInSec), countOnly);
        }
        else
        {
            var exampleSolver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, _formatter, _exampleCap);
            exampleSolver.UseCountOnlyUniqueMode = false;
            var examples = exampleSolver.Solve();
            return (examples, new SimulationResults([], 0, examples.ElapsedTimeInSec));
        }
    }
}
