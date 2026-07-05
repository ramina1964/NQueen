namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Enumeration")]
public class SolverSingleModeTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task GetSimResults_SingleMode_ExactlyOneSolutionMatchesExpected(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        solutionMode.ShouldBe(SolutionMode.Single);
        var ctx = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(ctx);
        expectedSolutions.ShouldHaveSingleItem();

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.ShouldBe(1UL,
            $"Single mode should return exactly one solution for N={boardSize}");

        results.Solutions.ShouldHaveSingleItem();
        var actualRows = results.Solutions[0].QueenPositions.ToArray();
        actualRows.ShouldBe(expectedSolutions[0], ignoreOrder: true);
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
