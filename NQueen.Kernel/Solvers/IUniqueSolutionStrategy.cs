namespace NQueen.Kernel.Solvers;

using NQueen.Domain.Models;

public interface IUniqueSolutionStrategy
{
    SimulationResults Solve(SimulationContext context, int exampleCap = 0);
}
