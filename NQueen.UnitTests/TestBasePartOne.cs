namespace NQueen.UnitTests;

public partial class TestBase
{
    public ISolverBackEnd? Sut { get; set; }

    public List<sbyte[]> ExpectedSolutions { get; set; } = new List<sbyte[]>();

    public List<sbyte[]> ActualSolutions { get; set; } = new List<sbyte[]>();

    public static List<sbyte[]> GetExpectedSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        return solutionMode == SolutionMode.Single
               ? GetExpectedSingleSolution(boardSize).ToList()
               : solutionMode == SolutionMode.Unique
               ? GetExpectedUniqueSolutions(boardSize).ToList()
               : GetExpectedAllSolutions(boardSize).ToList();
    }

    public List<sbyte[]> GetActualSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        return Sut
               .GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenList)
               .ToList();
    }

    protected static ISolver GenerateSut(sbyte boardSize, SolutionMode solutionMode)
    {
        var solutionDTO = new SolutionUpdateDTO { BoardSize = boardSize, SolutionMode = solutionMode };
        ISolutionDev solutionDev = new SolutionDev(solutionDTO);
        return new BackTracking(solutionDev);
    }
}
