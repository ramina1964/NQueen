namespace NQueen.Domain.Interfaces;

public interface ISolverBackEndPruning
{
    // Backend
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext);
}
