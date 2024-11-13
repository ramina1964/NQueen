namespace NQueen.UnitTests;

public partial class TestBase(ISolverBackEnd sut)
{
    public List<byte[]> ExpectedSolutions { get; set; } = [];

    public List<byte[]> ActualSolutions { get; set; } = [];

    public static List<byte[]> GetExpectedSolutions(byte boardSize, SolutionMode solutionMode)
    {
        return solutionMode == SolutionMode.Single
               ? new List<byte[]>(GetExpectedSingleSolution(boardSize))
               : solutionMode == SolutionMode.Unique
               ? new List<byte[]>(GetExpectedUniqueSolutions(boardSize))
               : new List<byte[]>(GetExpectedAllSolutions(boardSize));
    }

    public List<byte[]> GetActualSolutions(byte boardSize, SolutionMode solutionMode)
    {
        return Sut
               .GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenPositions)
               .ToList();
    }

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
