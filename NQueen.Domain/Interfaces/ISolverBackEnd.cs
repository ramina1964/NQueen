namespace NQueen.Domain.Interfaces;

public interface ISolverBackEnd
{
    // Backend
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext);
}
