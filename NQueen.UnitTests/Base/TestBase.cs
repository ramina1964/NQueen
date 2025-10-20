namespace NQueen.UnitTests.Base;

public class TestBase(ISolverBackEnd sut)
{
    public List<int[]> ExpectedSolutions { get; set; } = [];

    public List<int[]> ActualSolutions { get; set; } = [];

    public static List<int[]> FetchExpectedSols(SimulationContext simContext)
    {
        return simContext.SolutionMode switch
        {
            SolutionMode.Single => ExpectedSolutionData.SingleSolutions
                .TryGetValue(simContext.BoardSize, out var singleSolutions)
                ? singleSolutions
                : throw new KeyNotFoundException(
                    $"No single solutions found for board size {simContext.BoardSize}."),

            SolutionMode.Unique => ExpectedSolutionData.UniqueSolutions
                .TryGetValue(simContext.BoardSize, out var uniqueSolutions)
                ? uniqueSolutions
                : throw new KeyNotFoundException(
                    $"No unique solutions found for board size {simContext.BoardSize}."),

            SolutionMode.All => ExpectedSolutionData.AllSolutions
                .TryGetValue(simContext.BoardSize, out var allSolutions)
                ? allSolutions
                : throw new KeyNotFoundException(
                    $"No all solutions found for board size {simContext.BoardSize}."),

            _ => throw new ArgumentOutOfRangeException(nameof(simContext),
                    "Invalid solution mode.")
        };
    }

    public async Task<IEnumerable<int[]>> FetchActualSolsAsync(SimulationContext simContext) =>
        (await Sut.GetSimResultsAsync(simContext))
        .Solutions
        .Select(sol => sol.QueenPositions);

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));

    protected async Task AssertSolutionsAsync(SimulationContext simContext)
    {
        // Arrange
        ExpectedSolutions = FetchExpectedSols(simContext);

        // Act
        ActualSolutions = [.. await FetchActualSolsAsync(simContext)];

        SolutionAssertions.AssertSolutionsSetEquivalent(
            ActualSolutions,
            ExpectedSolutions,
            $"N={simContext.BoardSize} {simContext.SolutionMode}");
    }
}
