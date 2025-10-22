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
        solutionMode.Should().Be(SolutionMode.Single);
        var ctx = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(ctx);
        expectedSolutions.Should().ContainSingle(
            $"Expected data must hold exactly one solution for N={boardSize}");

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.Should().Be(1UL,
            $"Single mode should return exactly one solution for N={boardSize}");

        results.Solutions.Should().ContainSingle();
        var actualRows = results.Solutions[0].QueenPositions.ToArray();
        actualRows.Should().BeEquivalentTo(expectedSolutions[0]);
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
