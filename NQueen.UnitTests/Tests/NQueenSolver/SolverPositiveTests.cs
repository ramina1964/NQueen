namespace NQueen.UnitTests.Tests.NQueenSolver;

public class SolverPositiveTests : IClassFixture<SolverBackEndFixture>
{
    private readonly ISolverBackEnd _solver;

    public SolverPositiveTests(SolverBackEndFixture fixture)
    {
        _solver = fixture.Sut;
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateOneSingleSolution(
        int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);
        Assert.Single(expectedSolutions);

        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        actualSolutionsList.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedSolutions.First());
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfUniqueSolutions),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);

        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        SolutionAssertions.AssertSolutionsSetEquivalent(
            actualSolutionsList,
            expectedSolutions,
            $"N={boardSize} {solutionMode}");
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfAllSolutionsData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfAllSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);

        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        SolutionAssertions.AssertSolutionsSetEquivalent(
            actualSolutionsList,
            expectedSolutions,
            $"N={boardSize} {solutionMode}");
    }
}

