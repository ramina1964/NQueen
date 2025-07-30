namespace NQueen.Domain.Interfaces;

public interface ISolverBackEnd
{
    // Backend
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetResultsForBoardAsync( int boardSize,
        SolutionMode solutionMode, DisplayMode displayMode = DisplayMode.Hide);
}
