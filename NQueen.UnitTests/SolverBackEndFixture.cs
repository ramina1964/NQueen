namespace NQueen.UnitTests;

public class SolverBackEndFixture
{
    public ISolverBackEnd Sut { get; }

    public SolverBackEndFixture()
    {
        // Initialize the Sut with a default configuration
        Sut = GenerateSut(4, SolutionMode.All);
    }

    private static ISolverBackEnd GenerateSut(sbyte boardSize, SolutionMode solutionMode)
    {
        var solutionDTO = new SolutionUpdateDTO
        {
            BoardSize = boardSize,
            SolutionMode = solutionMode
        };

        ISolutionManager solutionManager = new SolutionManager(solutionDTO);
        return new BackTrackingSolver(solutionManager);
    }
}
