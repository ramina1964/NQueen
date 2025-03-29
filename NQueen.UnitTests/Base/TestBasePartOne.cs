namespace NQueen.UnitTests;

public partial class TestBase(ISolverBackEnd sut)
{
    public List<int[]> ExpectedSolutions { get; set; } = [];

    public List<int[]> ActualSolutions { get; set; } = [];

    public static List<int[]> GetExpectedSolutions(int boardSize, SolutionMode solutionMode) =>
        solutionMode == SolutionMode.Single
            ? [.. GetExpectedSingleSolution(boardSize)]
            : solutionMode == SolutionMode.Unique
            ? [.. GetExpectedUniqueSolutions(boardSize)]
            : [.. GetExpectedAllSolutions(boardSize)];

    public List<int[]> GetActualSolutions(int boardSize, SolutionMode solutionMode) => [.. Sut
               .GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenPositions)];

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
