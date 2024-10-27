namespace NQueen.UnitTests;

public partial class TestBase(ISolverBackEnd sut)
{
    public List<sbyte[]> ExpectedSolutions { get; set; } = [];

    public List<sbyte[]> ActualSolutions { get; set; } = [];

    public static List<sbyte[]> GetExpectedSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        return solutionMode == SolutionMode.Single
               ? new List<sbyte[]>(GetExpectedSingleSolution(boardSize))
               : solutionMode == SolutionMode.Unique
               ? new List<sbyte[]>(GetExpectedUniqueSolutions(boardSize))
               : new List<sbyte[]>(GetExpectedAllSolutions(boardSize));
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

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
