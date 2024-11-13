using NQueen.Kernel.Enums;
using NQueen.Kernel.Models;

namespace NQueen.Kernel.Interfaces;

public interface ISolverBackEnd
{
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetResultsAsync(byte boardSize, SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide);
}
