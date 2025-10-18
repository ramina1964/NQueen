namespace NQueen.UnitTests.Tests.NQueenSolver;

using NQueen.UnitTests.Fixtures;

/// <summary>
/// Verifies solver solution shape invariants: every returned solution must have
/// queenPositions.Length == requested board size and > 0.
/// Ensures the centralized ValidateRows logic in BitmaskSolver is effective across modes.
/// </summary>
[Collection("SolverBackend")]
public class SolverInvariantTests
{
    private readonly ISolverBackEnd _solver;

    public SolverInvariantTests(SolverBackEndFixture fixture) => _solver = fixture.Sut;

    [Theory]
    [MemberData(nameof(NQueenTestSets.SmallValueCases), MemberType = typeof(NQueenTestSets))]
    public async Task SolutionsHaveExpectedLength(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);

        foreach (var sol in results.Solutions)
        {
            var rows = sol.QueenPositions;
            rows.Should().NotBeNull();
            rows.Length.Should().Be(boardSize, $"Each solution must have length equal to board size (N={boardSize}).");
            rows.Length.Should().BeGreaterThan(0, "Solution arrays must be non-empty.");
        }
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldNotGenerateAnySolutionData), MemberType = typeof(NQueenTestSets))]
    public async Task BoardsWithoutSolutionsReturnEmptyList(int boardSize, SolutionMode mode)
    {
        var ctx = new SimulationContext(boardSize, mode, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.Should().BeEmpty("No solutions exist for N={boardSize} in mode {mode}.");
        results.SolutionsCount.Should().Be(0);
    }
}
