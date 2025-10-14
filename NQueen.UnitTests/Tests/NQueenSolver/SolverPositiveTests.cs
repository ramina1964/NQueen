namespace NQueen.UnitTests.Tests.NQueenSolver;

using NQueen.UnitTests.Fixtures;

[Collection("SolverBackend")]
public class SolverPositiveTests
{
    private readonly ISolverBackEnd _solver;

    private const int ExhaustiveSetVerificationMaxSize = 6; // full solution set equivalence only up to this size

    public SolverPositiveTests(SolverBackEndFixture fixture)
    {
        _solver = fixture.Sut;
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateOneSingleSolutionData), MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateOneSingleSolution(int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);
        Assert.Single(expectedSolutions);

        var actualResults = await _solver.GetSimResultsAsync(simContext);
        actualResults.Solutions.Should().ContainSingle();
        var actual = actualResults.Solutions[0].QueenPositions.ToArray();
        actual.Should().BeEquivalentTo(expectedSolutions[0]);
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfUniqueSolutions), MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);
        var actualResults = await _solver.GetSimResultsAsync(simContext);

        if (boardSize <= ExhaustiveSetVerificationMaxSize)
        {
            var actualSolutionsList = actualResults.Solutions
                .Select(s => s.QueenPositions.ToArray())
                .ToList();
            SolutionAssertions.AssertSolutionsSetEquivalent(actualSolutionsList, expectedSolutions, $"N={boardSize} {solutionMode}");
        }
        else
        {
            actualResults.SolutionsCount.Should().Be((ulong)expectedSolutions.Count, $"Unique count should match expected for N={boardSize}");
        }
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfAllSolutionsData), MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfAllSolutions(int boardSize, SolutionMode solutionMode)
    {
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);
        var actualResults = await _solver.GetSimResultsAsync(simContext);

        if (boardSize <= ExhaustiveSetVerificationMaxSize)
        {
            var actualSolutionsList = actualResults.Solutions
                .Select(s => s.QueenPositions.ToArray())
                .ToList();
            SolutionAssertions.AssertSolutionsSetEquivalent(actualSolutionsList, expectedSolutions, $"N={boardSize} {solutionMode}");
        }
        else
        {
            actualResults.SolutionsCount.Should().Be((ulong)expectedSolutions.Count, $"All-solutions count should match expected for N={boardSize}");
        }
    }
}

