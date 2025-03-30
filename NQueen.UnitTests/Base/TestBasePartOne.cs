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

    protected async Task<List<int[]>> GetActualSolutionsAsync(int boardSize, SolutionMode solutionMode)
    {
        try
        {
            var results = await Sut.GetResultsAsync(boardSize, solutionMode);
            return results.Solutions.Select(sol => sol.QueenPositions).ToList();
        }
        catch (Exception ex)
        {
            // Handle or log the exception as needed
            throw new InvalidOperationException("Failed to get actual solutions.", ex);
        }
    }

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
