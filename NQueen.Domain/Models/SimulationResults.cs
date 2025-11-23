namespace NQueen.Domain.Models;

public class SimulationResults
{
    // Preferred constructor: pass the real totalSolutions even if 'solutions' is truncated.
    public SimulationResults(IEnumerable<Solution> solutions,
        ulong totalSolutions, double ElapsedTimeInSec)
        : this(solutions, totalSolutions, ElapsedTimeInSec, inferred: false)
    { }

    // Legacy constructor (kept): total inferred from the (possibly truncated) solutions list.
    public SimulationResults(IEnumerable<Solution> solutions, double ElapsedTimeInSec)
        : this(solutions,
               solutions is ICollection<Solution> collection ? (ulong)collection.Count : (ulong)solutions.Count(),
               ElapsedTimeInSec,
               inferred: true)
    { }

    public IReadOnlyList<Solution> Solutions { get; }

    public ulong SolutionsCount { get; }

    public bool IsTotalInferred { get; }

    public double ElapsedTimeInSec { get; }

    public bool IsTruncated => SolutionsCount > (ulong)Solutions.Count;

    private SimulationResults(IEnumerable<Solution> solutions,
        ulong totalSolutions, double ElapsedTimeInSec, bool inferred)
    {
        var list = solutions.ToList();
        Solutions = list;
        SolutionsCount = totalSolutions;
        this.ElapsedTimeInSec = ElapsedTimeInSec;
        IsTotalInferred = inferred;
    }
}
