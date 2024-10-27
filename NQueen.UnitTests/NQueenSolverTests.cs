namespace NQueen.UnitTests;

public class NQueenSolverTests(SolverBackEndFixture fixture) :
    TestBase(fixture.Sut), IClassFixture<SolverBackEndFixture>
{
    [Theory]
    [InlineData(2, SolutionMode.Single)]
    [InlineData(3, SolutionMode.Single)]
    [InlineData(2, SolutionMode.Unique)]
    [InlineData(3, SolutionMode.Unique)]
    [InlineData(2, SolutionMode.All)]
    [InlineData(3, SolutionMode.All)]
    public void SolverShouldNotGenerateAnySolution(sbyte boardSize, SolutionMode solutionMode)
    {
        // Arrange
        ExpectedSolutions = GetExpectedSolutions(boardSize, solutionMode);

        // Act
        ActualSolutions = GetActualSolutions(boardSize, solutionMode);

        // Assert
        Assert.Equal(ExpectedSolutions.Count, ActualSolutions.Count);
    }

    [Theory]
    [InlineData(1, SolutionMode.Single)]
    [InlineData(1, SolutionMode.Unique)]
    [InlineData(1, SolutionMode.All)]
    [InlineData(4, SolutionMode.Single)]
    [InlineData(5, SolutionMode.Single)]
    [InlineData(6, SolutionMode.Single)]
    [InlineData(7, SolutionMode.Single)]
    [InlineData(8, SolutionMode.Single)]
    [InlineData(9, SolutionMode.Single)]
    [InlineData(10, SolutionMode.Single)]
    [InlineData(11, SolutionMode.Single)]
    [InlineData(12, SolutionMode.Single)]
    [InlineData(13, SolutionMode.Single)]
    [InlineData(18, SolutionMode.Single)]
    [InlineData(19, SolutionMode.Single)]
    [InlineData(20, SolutionMode.Single)]
    [InlineData(21, SolutionMode.Single)]
    [InlineData(22, SolutionMode.Single)]
    [InlineData(23, SolutionMode.Single)]
    [InlineData(24, SolutionMode.Single)]
    [InlineData(25, SolutionMode.Single)]
    [InlineData(26, SolutionMode.Single)]
    [InlineData(27, SolutionMode.Single)]
    [InlineData(28, SolutionMode.Single)]
    public void SolverShouldGenerateOneSingleSolution(sbyte boardSize, SolutionMode solutionMode)
    {
        // Arrange
        ExpectedSolutions = GetExpectedSolutions(boardSize, solutionMode);

        // Act
        ActualSolutions = GetActualSolutions(boardSize, solutionMode);

        // Assert: ExpectedSolutions and a ActualSolutions should contain the same item, regardless of the order of items
        Assert.Equal(ExpectedSolutions.Count, ActualSolutions.Count);
        ActualSolutions.Should().BeEquivalentTo(ExpectedSolutions);
    }

    [Theory]
    [InlineData(4, SolutionMode.Unique)]
    [InlineData(5, SolutionMode.Unique)]
    [InlineData(6, SolutionMode.Unique)]
    [InlineData(7, SolutionMode.Unique)]
    [InlineData(8, SolutionMode.Unique)]
    public void SolverShouldGenerateCorrectListOfUniqueSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        // Arrange
        ExpectedSolutions = GetExpectedSolutions(boardSize, solutionMode);

        // Act
        ActualSolutions = GetActualSolutions(boardSize, solutionMode);

        // Assert: ExpectedSolutions and a ActualSolutions should contain the same item, regardless of the order of items
        Assert.Equal(ExpectedSolutions.Count, ActualSolutions.Count);
        ActualSolutions.Should().BeEquivalentTo(ExpectedSolutions);
    }

    [Theory]
    [InlineData(4, SolutionMode.All)]
    [InlineData(5, SolutionMode.All)]
    [InlineData(6, SolutionMode.All)]
    [InlineData(7, SolutionMode.All)]
    [InlineData(8, SolutionMode.All)]
    public void SolverShouldGenerateCorrectListOfAllSolutions(sbyte boardSize, SolutionMode solutionMode)
    {
        // Arrange
        ExpectedSolutions = GetExpectedSolutions(boardSize, solutionMode);

        // Act
        ActualSolutions = GetActualSolutions(boardSize, solutionMode);

        // Assert
        Assert.Equal(ExpectedSolutions.Count, ActualSolutions.Count);
        ActualSolutions.Should().BeEquivalentTo(ExpectedSolutions);
    }
}
