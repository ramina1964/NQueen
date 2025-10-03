namespace NQueen.TestShared.Assertions;

public static class SolutionAssertions
{
    public static void AssertSolutionsSetEquivalent(
        IEnumerable<int[]> actual,
        IEnumerable<int[]> expected,
        string scenario)
    {
        var comparer = new StructuralIntArrayComparer();

        var expectedSet = new HashSet<int[]>(expected, comparer);
        var actualSet = new HashSet<int[]>(actual, comparer);

        actualSet.Count.Should().Be(expectedSet.Count,
            $"expected {expectedSet.Count} distinct solutions for {scenario} but got {actualSet.Count}");

        var missing = expectedSet.Where(e => !actualSet.Contains(e)).ToList();
        missing.Should().BeEmpty($"solver missed {missing.Count} solution(s) for {scenario}");

        var unexpected = actualSet.Where(a => !expectedSet.Contains(a)).ToList();
        unexpected.Should().BeEmpty($"solver produced {unexpected.Count} unexpected solution(s) for {scenario}");
    }

    private sealed class StructuralIntArrayComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y) =>
            ReferenceEquals(x, y) ||
            (x is not null && y is not null && x.Length == y.Length && x.AsSpan().SequenceEqual(y));

        public int GetHashCode(int[] obj)
        {
            unchecked
            {
                var hash = 17;
                foreach (var v in obj) hash = hash * 31 + v;
                return hash;
            }
        }
    }
}
