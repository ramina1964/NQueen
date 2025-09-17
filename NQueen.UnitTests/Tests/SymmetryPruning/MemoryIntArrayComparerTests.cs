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


    [Theory]
    [InlineData(new int[] { 0, 2, 4, 1, 3 }, 7)]
    [InlineData(new int[] { 1, 3, 0, 2, 4 }, 7)]
    public void GetSymmetricalTransformations_ShouldReturnExpectedCount(int[] solution, int expectedCount)
    {
        var variants = SymmetryHelper.GetSymmetricalSolutions(solution);
        variants.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void GetSymmetricalTransformations_ShouldReturnAllExpectedVariants()
    {
        // Arrange
        var solution = new int[] { 0, 2, 4, 1, 3 };
        int[][] expectedVariants = [
            [ 0, 2, 4, 1, 3 ],
            [ 3, 1, 4, 2, 0 ],
            [ 4, 2, 0, 3, 1 ],
            [ 1, 3, 0, 2, 4 ],
            [ 2, 0, 3, 1, 4 ],
            [ 4, 1, 3, 0, 2 ],
            [ 2, 4, 1, 3, 0 ]
        ];

        // Act
        var variants = SymmetryHelper.GetSymmetricalSolutions(solution);

        // Assert
        foreach (var expected in expectedVariants)
        {
            variants.Should().ContainEquivalentOf(expected,
                $"Expected variant {string.Join(",", expected)} should be present.");
        }
        variants.Should().HaveCount(expectedVariants.Length);
    }
}
