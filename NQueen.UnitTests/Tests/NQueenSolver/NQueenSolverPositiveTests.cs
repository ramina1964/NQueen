namespace NQueen.UnitTests.Tests.NQueenSolver;

public class NQueenSolverPositiveTests : IDisposable
{
    private readonly ISolverBackEnd _solverBackEnd;
    private readonly ServiceProvider _serviceProvider;

    public NQueenSolverPositiveTests()
    {
        // Initialize a new service provider for each test
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddTestServices();
        _serviceProvider = services.BuildServiceProvider();

        // Resolve the ISolverBackEnd dependency
        _solverBackEnd = _serviceProvider.GetRequiredService<ISolverBackEnd>();
    }

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateOneSingleSolution(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);
        Assert.Single(expectedSolutions); // Ensure there is exactly one expected solution

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsAsync(boardSize, solutionMode);

        // Convert actualSolutions.Solutions to a list of int[] for comparison
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        // Check wether 'actualSolutionsList' contains one solution equal to the expected solution
        actualSolutionsList.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedSolutions.First());
    }

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateCorrectListOfUniqueSolutions),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsAsync(boardSize, solutionMode);

        // Convert actualSolutions.Solutions to a list of int[] for comparison
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        // Assert: Use order-insensitive comparison
        Assert.Equal(
            expectedSolutions.OrderBy(solution => string.Join(",", solution)),
            actualSolutionsList.OrderBy(solution => string.Join(",", solution))
        );
    }

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateCorrectListOfAllSolutionsData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateCorrectListOfAllSolutions(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var expectedSolutions = NQueenTestBase.FetchExpectedSols(boardSize, solutionMode);

        // Act
        var actualSolutions = await _solverBackEnd.GetResultsAsync(boardSize, solutionMode);

        // Convert actualSolutions.Solutions to a list of int[] for comparison
        var actualSolutionsList = actualSolutions.Solutions
            .Select(solution => solution.QueenPositions.ToArray())
            .ToList();

        // Assert: Use order-insensitive comparison
        Assert.Equal(
            expectedSolutions.OrderBy(solution => string.Join(",", solution)),
            actualSolutionsList.OrderBy(solution => string.Join(",", solution))
        );
    }

    public void Dispose()
    {
        // Dispose of the service provider to clean up resources
        _serviceProvider.Dispose();
    }
}
