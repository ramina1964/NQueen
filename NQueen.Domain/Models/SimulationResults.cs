namespace NQueen.Domain.Models;

public record SimulationResults(
    IEnumerable<Solution> Solutions,
    double ElapsedTimeInSec);
