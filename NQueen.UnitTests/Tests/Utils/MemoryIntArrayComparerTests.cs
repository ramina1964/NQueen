namespace NQueen.UnitTests.Tests.Utils;

public class MemoryIntArrayComparerTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_ForIdenticalArrays()
    {
        // Arrange
        var comparer = new MemoryIntArrayComparer();
        var array1 = new Memory<int>(new[] { 4, 2, 0, 3, 1 });
        var array2 = new Memory<int>(new[] { 4, 2, 0, 3, 1 });

        // Act
        var areEqual = comparer.Equals(array1, array2);

        // Assert
        areEqual.Should().BeTrue("Arrays with identical contents should be equal.");
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentArrays()
    {
        // Arrange
        var comparer = new MemoryIntArrayComparer();
        var array1 = new Memory<int>(new[] { 4, 2, 0, 3, 1 });
        var array2 = new Memory<int>(new[] { 1, 3, 0, 2, 4 });

        // Act
        var areEqual = comparer.Equals(array1, array2);

        // Assert
        areEqual.Should().BeFalse("Arrays with different contents should not be equal.");
    }
}