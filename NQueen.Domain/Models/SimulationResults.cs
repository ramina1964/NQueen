namespace NQueen.Domain.Models;

public class SimulationResults
{
    public SimulationResults(IEnumerable<Solution> solutions)
    {
        Debug.Assert(solutions != null, "solutions != null");
        Solutions = [.. solutions];
    }

    public IEnumerable<Solution> Solutions { get; set; }

    public double ElapsedTimeInSec { get; set; }
}
