using NQueen.Kernel.Enums;
using NQueen.Kernel.Models;

namespace NQueen.Kernel.Interfaces;

public interface ISolverBackEnd
{
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetResultsAsync(sbyte boardSize, SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide);
}
