namespace NQueen.UnitTests;

public partial class TestBase
{
    public ISolverBackEnd? Sut { get; set; }

    public List<sbyte[]> ExpectedSolutions { get; set; } = [];

    public List<sbyte[]> ActualSolutions { get; set; } = [];

    public static List<sbyte[]> GetExpectedSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        return solutionMode == SolutionMode.Single
               ? [.. GetExpectedSingleSolution(boardSize)]
               : solutionMode == SolutionMode.Unique
               ? [.. GetExpectedUniqueSolutions(boardSize)]
               : [.. GetExpectedAllSolutions(boardSize)];
    }

    public List<sbyte[]> GetActualSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        return Sut
               !.GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenList)
               .ToList();
    }

    protected static ISolver GenerateSut(sbyte boardSize, SolutionMode solutionMode)
    {
        var solutionDTO = new SolutionUpdateDTO { BoardSize = boardSize, SolutionMode = solutionMode };
        ISolutionManager solutionDeveloper = new SolutionManager(solutionDTO);
        return new BackTrackingSolver(solutionDeveloper);
    }
}
