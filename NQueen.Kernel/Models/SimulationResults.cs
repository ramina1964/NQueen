namespace NQueen.Kernel.Models;

public class SimulationResults
{
    public SimulationResults(IEnumerable<Solution> solutions)
    {
        Debug.Assert(solutions != null, "allSolutions != null");
        var enumerable = solutions as IList<Solution> ?? [.. solutions];
        var sol = enumerable.FirstOrDefault();
        if (sol == null)
        {
            NoOfSolutions = 0;
            Solutions = [];
        }
        else
        {
            BoardSize = sol.Positions.Count;
            NoOfSolutions = enumerable.Count;
            Solutions = [.. enumerable];
        }
    }

    public int BoardSize { get; set; }

    public int NoOfSolutions { get; set; }

    public IEnumerable<Solution> Solutions { get; set; }

    public double ElapsedTimeInSec { get; set; }
}
