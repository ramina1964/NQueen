namespace NQueen.TestShared.Assertions;

public static class SolutionAssertions
{
    public static void AssertSolutionsSetEquivalent(
        IEnumerable<int[]> actual,
        IEnumerable<int[]> expected,
        string scenario)
    {
        var comparer = IntArrayStructuralComparer.Instance;

        var expectedSet = new HashSet<int[]>(expected, comparer);
        var actualSet = new HashSet<int[]>(actual, comparer);

        actualSet.Count.Should().Be(expectedSet.Count,
            $"expected {expectedSet.Count} distinct solutions for {scenario} but got {actualSet.Count}");

        var missing = expectedSet.Where(e => !actualSet.Contains(e)).ToList();
        missing.Should().BeEmpty($"solver missed {missing.Count} solution(s) for {scenario}");

        var unexpected = actualSet.Where(a => !expectedSet.Contains(a)).ToList();
        unexpected.Should().BeEmpty($"solver produced {unexpected.Count} unexpected solution(s) for {scenario}");
    }
}
