namespace NQueen.UnitTests.Base;

public class TestBase(ISolverBackEnd sut)
{
    public List<UInt128> ExpectedSolutionsPacked { get; set; } = [];

    public List<UInt128> ActualSolutionsPacked { get; set; } = [];

    // Legacy helper for tests expecting raw int[] solutions
    public static List<int[]> FetchExpectedSols(SimulationContext simContext)
    {
        return simContext.SolutionMode switch
        {
            SolutionMode.Single => NQueen.Domain.Utils.ExpectedSolutionData.SingleSolutions
                .TryGetValue(simContext.BoardSize, out var singleSolutions)
                ? singleSolutions
                : throw new KeyNotFoundException(
                    $"No single solutions found for board size {simContext.BoardSize}."),

            SolutionMode.Unique => NQueen.Domain.Utils.ExpectedSolutionData.UniqueSolutions
                .TryGetValue(simContext.BoardSize, out var uniqueSolutions)
                ? uniqueSolutions
                : throw new KeyNotFoundException(
                    $"No unique solutions found for board size {simContext.BoardSize}."),

            SolutionMode.All => NQueen.Domain.Utils.ExpectedSolutionData.AllSolutions
                .TryGetValue(simContext.BoardSize, out var allSolutions)
                ? allSolutions
                : throw new KeyNotFoundException(
                    $"No all solutions found for board size {simContext.BoardSize}."),

            _ => throw new ArgumentOutOfRangeException(nameof(simContext),
                    "Invalid solution mode.")
        };
    }

    public static List<UInt128> FetchExpectedSolsPacked(SimulationContext simContext)
    {
        return simContext.SolutionMode switch
        {
            SolutionMode.Single => NQueen.TestShared.Data.ExpectedSolutions.SinglePacked
                .TryGetValue(simContext.BoardSize, out var singleSolutions)
                ? singleSolutions
                : throw new KeyNotFoundException(
                    $"No single solutions found for board size {simContext.BoardSize}."),

            SolutionMode.Unique => NQueen.TestShared.Data.ExpectedSolutions.UniquePacked
                .TryGetValue(simContext.BoardSize, out var uniqueSolutions)
                ? uniqueSolutions
                : throw new KeyNotFoundException(
                    $"No unique solutions found for board size {simContext.BoardSize}."),

            SolutionMode.All => NQueen.TestShared.Data.ExpectedSolutions.AllPacked
                .TryGetValue(simContext.BoardSize, out var allSolutions)
                ? allSolutions
                : throw new KeyNotFoundException(
                    $"No all solutions found for board size {simContext.BoardSize}."),

            _ => throw new ArgumentOutOfRangeException(nameof(simContext),
                    "Invalid solution mode.")
        };
    }

    public async Task<IEnumerable<UInt128>> FetchActualSolsPackedAsync(SimulationContext simContext) =>
        (await Sut.GetSimResultsAsync(simContext))
        .Solutions
        .Select(sol => NQueen.Domain.Utils.SymmetryHelper.PackRows(sol.QueenPositions));

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));

    protected async Task AssertSolutionsPackedAsync(SimulationContext simContext)
    {
        // Arrange
        ExpectedSolutionsPacked = FetchExpectedSolsPacked(simContext);

        // Act
        ActualSolutionsPacked = [.. await FetchActualSolsPackedAsync(simContext)];

        // Compare sets via HashSet equality since packed assertion helper does not exist.
        var expectedSet = new HashSet<UInt128>(ExpectedSolutionsPacked);
        var actualSet = new HashSet<UInt128>(ActualSolutionsPacked);
        actualSet.Count.Should().Be(expectedSet.Count,
            $"expected {expectedSet.Count} distinct solutions for N={simContext.BoardSize} {simContext.SolutionMode} but got {actualSet.Count}");
        var missing = expectedSet.Except(actualSet).ToList();
        missing.Should().BeEmpty($"solver missed {missing.Count} solution(s) for N={simContext.BoardSize} {simContext.SolutionMode}");
        var unexpected = actualSet.Except(expectedSet).ToList();
        unexpected.Should().BeEmpty($"solver produced {unexpected.Count} unexpected solution(s) for N={simContext.BoardSize} {simContext.SolutionMode}");
    }
}
