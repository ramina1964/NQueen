namespace NQueen.UnitTests.Tests;

public class NQueenSolverTests(SolverBackEndFixture fixture) :
    NQueenTestBase(fixture.Sut), IClassFixture<SolverBackEndFixture>
{
    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestData))] 
    public async Task SolverShouldNotGenerateAnySolution(int boardSize, SolutionMode solutionMode) =>
        await AssertSolutionsAsync(boardSize, solutionMode);

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateOneSingleSolutionData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateOneSingleSolution(int boardSize, SolutionMode solutionMode)
        => await AssertSolutionsAsync(boardSize, solutionMode);

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateCorrectListOfUniqueSolutions),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateCorrectListOfUniqueSolutions(int boardSize, SolutionMode solutionMode)
        => await AssertSolutionsAsync(boardSize, solutionMode);

    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldGenerateCorrectListOfAllSolutionsData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldGenerateCorrectListOfAllSolutions(int boardSize, SolutionMode solutionMode)
        => await AssertSolutionsAsync(boardSize, solutionMode);
}

