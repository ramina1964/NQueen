namespace NQueen.UnitTests.Tests.Domain;

public class ExpectedSolutionCountsGetTests
{
    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(8, 92UL)]
    [InlineData(10, 724UL)]
    [InlineData(0, 0UL)]
    [InlineData(999, 0UL)]
    public void GetAll_ReturnsKnownValuesOrZero(int n, ulong expected) =>
        ExpectedSolutionCounts.GetAll(n).ShouldBe(expected);

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(6, 1UL)]
    [InlineData(10, 92UL)]
    [InlineData(0, 0UL)]
    [InlineData(999, 0UL)]
    public void GetUnique_ReturnsKnownValuesOrZero(int n, ulong expected) =>
        ExpectedSolutionCounts.GetUnique(n).ShouldBe(expected);
}
