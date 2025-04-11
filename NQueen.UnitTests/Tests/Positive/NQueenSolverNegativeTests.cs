namespace NQueen.UnitTests.Tests.Positive;

public class NQueenSolverNegativeTests(SolverBackEndFixture fixture) :
    NQueenTestBase(fixture.Sut), IClassFixture<SolverBackEndFixture>
{
    [Theory]
    [MemberData(nameof(NQueenTestData.SolverShouldNotGenerateAnySolutionData),
        MemberType = typeof(NQueenTestData))]
    public async Task SolverShouldNotGenerateAnySolution(int boardSize, SolutionMode solutionMode) =>
        await AssertSolutionsAsync(boardSize, solutionMode);
}