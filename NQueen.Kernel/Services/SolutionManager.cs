namespace NQueen.Kernel.Services;

public class SolutionManager : ISolutionManager
{
    public SolutionManager() { }

    public SolutionManager(SolutionUpdateDTO dto) => UpdateDTO = dto;

    public event Action<SolutionUpdateDTO> SolutionsUpdated;

    public void UpdateSolutions(SolutionUpdateDTO solutionUpdateDTO)
    {
        var queenPositions = solutionUpdateDTO.QueenPositions;

        // For SolutionMode.Single:
        if (solutionUpdateDTO.SolutionMode == SolutionMode.Single)
        {
            solutionUpdateDTO.Solutions.Add(queenPositions);
            SolutionsUpdated?.Invoke(solutionUpdateDTO);
            return;
        }

        // For each solution find all symmetrical counterparts, i.e., a list of maximum
        // eight items including the solution itself.
        var symmetricalSolutions = SolverHelper.GetSymmetricalSolutions(queenPositions);

        // For SolutionMode.All: Add this solution and all its symmetrical counterparts to the Solutions.
        if (solutionUpdateDTO.SolutionMode == SolutionMode.All)
        {
            solutionUpdateDTO.Solutions.UnionWith(symmetricalSolutions);
            SolutionsUpdated?.Invoke(solutionUpdateDTO);
            return;
        }

        // For SolutionMode.Unique: Add this solution to Solutions only if no overlaps between Solutions
        // and symmetricalSolutions are found. Note that it is more efficient to have the larger collection
        // as the outer variable and the smaller as the argument of Overlap().
        if (solutionUpdateDTO.Solutions.Overlaps(symmetricalSolutions) == false)
        {
            solutionUpdateDTO.Solutions.Add(queenPositions);
            SolutionsUpdated?.Invoke(solutionUpdateDTO);
        }
    }

    public SolutionUpdateDTO UpdateDTO { get; }
}
