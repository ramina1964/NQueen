namespace NQueen.UnitTests.Tests.SymmetryPruning;

// Todo : Find out which of these methods has the best resource management and performance.
public class MemoryIntArrayComparerTests
{
    [Theory]
    [InlineData(new int[] { 4, 2, 0, 3, 1 }, new int[] { 4, 2, 0, 3, 1 }, true)]
    [InlineData(new int[] { 4, 2, 0, 3, 1 }, new int[] { 1, 3, 0, 2, 4 }, false)]
    [InlineData(new int[] { 0, 1, 2 }, new int[] { 0, 1, 2 }, true)]
    [InlineData(new int[] { 0, 1, 2 }, new int[] { 2, 1, 0 }, false)]
    public void Equals_ShouldCompareArraysCorrectly(int[] first, int[] second, bool expected)
    {
        var comparer = new MemoryIntArrayComparer();
        var memoryA = new Memory<int>(first);
        var memoryB = new Memory<int>(second);

        var areEqual = comparer.Equals(memoryA, memoryB);

        areEqual.Should().Be(expected, $"comparing {string.Join(",", first)} and {string.Join(",", second)} should be {expected}");
    }

    [Theory]
    [InlineData(new int[] { 0, 2, 4, 1, 3 }, 8)]
    [InlineData(new int[] { 1, 3, 0, 2, 4 }, 8)]
    public void GetSymmetricalTransformations_ShouldReturnExpectedCount(
        int[] solutionArray, int expectedCount)
    {
        var solution = new Memory<int>(solutionArray);
        var variants = SymmetryHelper.GetSymmetricalSolutions(solution);
        variants.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void GetSymmetricalTransformations_ShouldReturnAllExpectedVariants()
    {
        // Arrange
        var solution = new int[] { 0, 2, 4, 1, 3 };
        int[][] expectedVariants =
        [
            [ 0, 2, 4, 1, 3 ],
            [ 3, 1, 4, 2, 0 ],
            [ 4, 2, 0, 3, 1 ],
            [ 1, 3, 0, 2, 4 ],
            [ 2, 0, 3, 1, 4 ],
            [ 4, 1, 3, 0, 2 ],
            [ 2, 4, 1, 3, 0 ],
            [ 0, 3, 1, 4, 2 ]
        ];

        // Act
        var variants = SymmetryHelper.GetSymmetricalSolutions(solution);

        // Assert (set equivalence, unordered, no dups, no missing)
        SolutionAssertions.AssertSolutionsSetEquivalent(
            variants,
            expectedVariants,
            "symmetry variants N=5");
    }
}
