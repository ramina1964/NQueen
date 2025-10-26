namespace NQueen.UnitTests.Tests.SymmetryPruning;

public class MemoryIntArrayComparerTests
{
    private static readonly MemoryIntArrayComparer Comparer = MemoryIntArrayComparer.Instance;

    [Theory]
    [MemberData(nameof(ExpectedSolutions.MemoryComparerEqualityCases), MemberType = typeof(ExpectedSolutions))]
    public void Equals_GivenTwoMemoryWrappedArrays_ReturnsExpectedResult(int[] first, int[] second, bool expected)
    {
        var memoryA = new Memory<int>(first);
        var memoryB = new Memory<int>(second);

        var areEqual = Comparer.Equals(memoryA, memoryB);

        areEqual.Should().Be(expected, $"comparing {string.Join(',', first)} and {string.Join(',', second)} should be {expected}");
    }

    [Theory]
    [MemberData(nameof(ExpectedSolutions.N5BaseSolutions), MemberType = typeof(ExpectedSolutions))]
    public void GetSymmetricalSolutions_N5_BaseSolutions_ReturnsExpectedVariantCount(int[] solutionArray)
    {
        var solution = new Memory<int>(solutionArray);
        var variants = GetSymmetricalSolutions(solution);
        variants.Should().HaveCount(ExpectedSolutions.ExpectedSymmetryVariantCountN5);
    }

    [Fact]
    public void GetSymmetricalSolutions_N5_BaseSolution_ReturnsAllExpectedVariants()
    {
        var solution = ExpectedSolutions.N5Base;
        int[][] expectedVariants =
        [
            [0, 2, 4, 1, 3],
            [3, 1, 4, 2, 0],
            [4, 2, 0, 3, 1],
            [1, 3, 0, 2, 4],
            [2, 0, 3, 1, 4],
            [4, 1, 3, 0, 2],
            [2, 4, 1, 3, 0],
            [0, 3, 1, 4, 2]
        ];

        var variants = GetSymmetricalSolutions(solution);

        SolutionAssertions.AssertSolutionsSetEquivalent(
            variants,
            expectedVariants,
            "symmetry variants N=5");
    }

    private static List<int[]> GetSymmetricalSolutions(int[] solution)
    {
        var transforms = SymmetryHelper.GetAllTransforms(solution);
        return transforms.Select(t => t.ToArray()).ToList();
    }

    private static List<int[]> GetSymmetricalSolutions(Memory<int> solution) => GetSymmetricalSolutions(solution.ToArray());
    private static List<int[]> GetSymmetricalSolutions(ReadOnlySpan<int> solution) => GetSymmetricalSolutions(solution.ToArray());
}
