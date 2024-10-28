using NQueen.Kernel.Models;

namespace NQueen.Kernel.Interfaces;

public interface ISolutionManager
{
    void UpdateSolutions(SolutionUpdateDTO solutionUpdateDTO);
}
