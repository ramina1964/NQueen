namespace NQueen.Kernel;

public class SolutionDev : ISolutionDev
{
    public SolutionDev() { } 

    // This is used in unit testing to send in board size and solution mode.
    public SolutionDev(SolutionUpdateDTO dto) => UpdateDTO = dto;

    public void UpdateSolutions(SolutionUpdateDTO dto)
    {
        var queenList = dto.QueenList;

        // For SolutionMode.Single:
        if (dto.SolutionMode == SolutionMode.Single)
        {
            dto.Solutions.Add(queenList);

            return;
        }

        // For each solution find all symmetrical counterparts, i.e., a list of maximum eight items included the solution itself.
        var symmetricalSolutions = Utility.GetSymmetricalSolutions(queenList);

        // For SolutionMode.All, add this solution and all its symmetrical counterparts to Solutions.
        if (dto.SolutionMode == SolutionMode.All)
        {
            dto.Solutions.UnionWith(symmetricalSolutions);
            return;
        }

        // For SolutionMode.Unique: Add this solution to Solutions only if no overlaps between Solutions and symmetricalSolutions are found.
        if (dto.Solutions.Overlaps(symmetricalSolutions) == false)
            dto.Solutions.Add(queenList);
    }

    public SolutionUpdateDTO UpdateDTO { get; }
}
