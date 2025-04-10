namespace NQueen.Kernel.Interfaces;

public interface ISolverBackEnd
{
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetResultsAsync(int boardSize, SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide);
}
