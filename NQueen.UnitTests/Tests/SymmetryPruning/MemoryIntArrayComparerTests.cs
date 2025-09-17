namespace NQueen.UnitTests.Tests.SymmetryPruning;

public class MemoryIntArrayComparerTests
{
    [Theory]
    [InlineData(new int[] { 4, 2, 0, 3, 1 }, new int[] { 4, 2, 0, 3, 1 }, true)]
    [InlineData(new int[] { 4, 2, 0, 3, 1 }, new int[] { 1, 3, 0, 2, 4 }, false)]
    [InlineData(new int[] { 0, 1, 2 }, new int[] { 0, 1, 2 }, true)]
    [InlineData(new int[] { 0, 1, 2 }, new int[] { 2, 1, 0 }, false)]
    public void Equals_ShouldCompareArraysCorrectly(int[] first, int[] second, bool expected)
    {
        // Arrange
        var comparer = new MemoryIntArrayComparer();
        var memoryA = new Memory<int>(first);
        var memoryB = new Memory<int>(second);

        // Act
        var areEqual = comparer.Equals(memoryA, memoryB);

        // Assert
        areEqual.Should().Be(expected, $"comparing {string.Join(",", first)} and" +
            $"{string.Join(",", second)} should be {expected}");
    }
}
