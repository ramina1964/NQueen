namespace NQueen.Domain.Models;

public class SimulationResults
{
    // Preferred constructor: pass the real totalSolutions even if 'solutions' is truncated.
    // Parameter name 'ElapsedTimeInSec' preserved.
    public SimulationResults(IEnumerable<Solution> solutions,
        int totalSolutions, double ElapsedTimeInSec)
        : this(solutions, totalSolutions, ElapsedTimeInSec, inferred: false)
    {}

    // Legacy constructor (kept): total inferred from the (possibly truncated) solutions list.
    // Parameter name 'ElapsedTimeInSec' preserved for backward compatibility with tests.
    public SimulationResults(IEnumerable<Solution> solutions, double ElapsedTimeInSec)
        : this(solutions,
               solutions is ICollection<Solution> coll ? coll.Count : solutions.Count(),
               ElapsedTimeInSec,
               inferred: true)
    {}

    public IReadOnlyList<Solution> Solutions { get; }

    public int SolutionsCount { get; }

    public bool IsTotalInferred { get; }

    public double ElapsedTimeInSec { get; }

    public bool IsTruncated => SolutionsCount > Solutions.Count;

    private SimulationResults(IEnumerable<Solution> solutions,
        int totalSolutions, double ElapsedTimeInSec, bool inferred)
    {
        var list = solutions.ToList();
        Solutions = list;
        SolutionsCount = totalSolutions;
        this.ElapsedTimeInSec = ElapsedTimeInSec;
        IsTotalInferred = inferred;
    }
}
