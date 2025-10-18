namespace NQueen.Domain.Interfaces;

public interface ISolverBackEnd
{
    // Backend control & modes
    bool IsSolverCanceled { get; set; }

    // Count-only mode flags (moved from ISolver to consolidate backend concerns)
    bool UseCountOnlyUniqueMode { get; set; }
    
    bool UseCountOnlyAllMode   { get; set; }

    Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext);
}
