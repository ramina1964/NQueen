namespace NQueen.UnitTests.Tests.NQueenSolver;

public class SolverPositiveTests : IDisposable
{
    public SolverPositiveTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();
        _serviceProvider = services.BuildServiceProvider();
        _solver = _serviceProvider.GetRequiredService<ISolverBackEnd>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateOneSingleSolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);
        Assert.Single(expectedSolutions);

        // Act
        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray());

        // Assert
        actualSolutionsList.Should().ContainSingle().Which.Should()
            .BeEquivalentTo(expectedSolutions.First());
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfUniqueSolutions),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);

        // Act
        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray());

        // Assert
        Assert.Equal(
            expectedSolutions.OrderBy(solution => string.Join(",", solution)),
            actualSolutionsList.OrderBy(solution => string.Join(",", solution))
        );
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfAllSolutionsData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfAllSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var simContext = new SimulationContext(boardSize, solutionMode, DisplayMode.Hide);
        var expectedSolutions = TestBase.FetchExpectedSols(simContext);

        // Act
        var actualResults = await _solver.GetSimResultsAsync(simContext);
        var actualSolutionsList = actualResults.Solutions
            .Select(solution => solution.QueenPositions.ToArray());

        // Assert
        Assert.Equal(
            expectedSolutions
            .OrderBy(solution => string.Join(",", solution)),
            actualSolutionsList
            .OrderBy(solution => string.Join(",", solution))
        );
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private readonly ISolverBackEnd _solver;
    private readonly ServiceProvider _serviceProvider;
}

