namespace NQueen.UnitTests;

public class SolverBackEndFixture : IClassFixture<SolverBackEndFixture>
{
    public ISolverBackEnd Sut { get; }

    public SolverBackEndFixture()
    {
        var solutionDTO = new SolutionUpdateDTO
        {
            BoardSize = 4,
            SolutionMode = SolutionMode.All
        };

        ISolutionManager solutionManager = new SolutionManager(solutionDTO);
        var solver = new BackTrackingSolver(solutionManager);

        Sut = solver ?? throw new ArgumentNullException(nameof(solver));
    }
}