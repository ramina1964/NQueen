namespace NQueen.UnitTests.Base;

public class NQueenTestBase(ISolverBackEnd sut)
{
    public List<int[]> ExpectedSolutions { get; set; } = [];

    public List<int[]> ActualSolutions { get; set; } = [];

    public static List<int[]> FetchExpectedSols(int boardSize, SolutionMode solutionMode) =>
        solutionMode switch
        {
            SolutionMode.Single => ExpectedSolutionData.SingleSolutions
                .TryGetValue(boardSize, out var singleSolutions)
                ? singleSolutions
                : throw new KeyNotFoundException($"No single solutions found for board size {boardSize}."),

            SolutionMode.Unique => ExpectedSolutionData.UniqueSolutions
                .TryGetValue(boardSize, out var uniqueSolutions)
                ? uniqueSolutions
                : throw new KeyNotFoundException($"No unique solutions found for board size {boardSize}."),

            SolutionMode.All => ExpectedSolutionData.
                AllSolutions
                .TryGetValue(boardSize, out var allSolutions)
                ? allSolutions
                : throw new KeyNotFoundException($"No all solutions found for board size {boardSize}."),

            _ => throw new ArgumentOutOfRangeException(nameof(solutionMode), "Invalid solution mode.")
        };

    public async Task<IEnumerable<int[]>> FetchActualSolsAsync(int boardSize, SolutionMode solutionMode) =>
        (await Sut.GetResultsAsync(boardSize, solutionMode)).Solutions
            .Select(sol => sol.QueenPositions);

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));
}
