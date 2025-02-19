namespace NQueen.UnitTests;

public partial class TestBase(ISolverBackEnd sut)
{
    public List<byte[]> ExpectedSolutions { get; set; } = [];

    public List<byte[]> ActualSolutions { get; set; } = [];

    public static List<byte[]> GetExpectedSolutions(byte boardSize, SolutionMode solutionMode) => solutionMode == SolutionMode.Single
               ? [.. GetExpectedSingleSolution(boardSize)]
               : solutionMode == SolutionMode.Unique
               ? [.. GetExpectedUniqueSolutions(boardSize)]
               : [.. GetExpectedAllSolutions(boardSize)];

    public List<byte[]> GetActualSolutions(byte boardSize, SolutionMode solutionMode) => [.. Sut
               .GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenPositions)];

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
