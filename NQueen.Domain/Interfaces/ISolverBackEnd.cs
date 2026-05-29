namespace NQueen.Domain.Interfaces;

public interface ISolverBackEnd
{
    bool IsSolverCanceled { get; set; }

    // When true, skips solution materialization and only tracks the total count.
    // These flags are the canonical way to request count-only mode; the solver's
    // ResultStorageMode properties (AllStorageMode / UniqueStorageMode) are kept
    // in sync by the ViewModel but these flags take precedence inside Solve().
    bool UseCountOnlyUniqueMode { get; set; }

    bool UseCountOnlyAllMode { get; set; }

    Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext);
}
