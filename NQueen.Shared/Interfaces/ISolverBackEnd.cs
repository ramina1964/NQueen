namespace NQueen.Shared.Interfaces;

public interface ISolverBackEnd
{
    bool CancelSolver { get; set; }

    Task<SimulationResults> GetResultsAsync(sbyte boardSize, SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide);
}
