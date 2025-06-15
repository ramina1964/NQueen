namespace NQueen.UnitTests.Tests.NQueenSolver;

public class NQueenSolverPositiveTests : IDisposable
{
    public NQueenSolverPositiveTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();
        _serviceProvider = services.BuildServiceProvider();

        _solverBackEnd = _serviceProvider.GetRequiredService<ISolver>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateOneSingleSolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);
        Assert.Single(expectedSolutions);

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsForBoardAsync(boardSize, solutionMode);
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        // Assert
        actualSolutionsList.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedSolutions.First());
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SolverShouldGenerateCorrectListOfUniqueSolutions),
        MemberType = typeof(NQueenTestSets))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsForBoardAsync(boardSize, solutionMode);
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

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
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsForBoardAsync(boardSize, solutionMode);
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        // Assert
        Assert.Equal(
            expectedSolutions.OrderBy(solution => string.Join(",", solution)),
            actualSolutionsList.OrderBy(solution => string.Join(",", solution))
        );
    }

    public void Dispose() => _serviceProvider.Dispose();
    
    private readonly ISolver _solverBackEnd;
    private readonly ServiceProvider _serviceProvider;
}

