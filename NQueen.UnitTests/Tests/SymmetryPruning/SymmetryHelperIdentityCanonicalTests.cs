namespace NQueen.UnitTests.Tests.SymmetryPruning;

[Collection("SolverBackend")]
[Trait("Category", "Canonical")]
public class SymmetryHelperIdentityCanonicalTests(SolverBackEndFixture fixture) :
    TestBase(fixture.Sut)
{
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void IsIdentityCanonical_ReturnsTrue_ForCanonicalSolutions(int n)
    {
        var all = EnumerateAllRawSolutions(n).ToList();
        all.Should().NotBeEmpty();
        foreach (var sol in all.Take(3))
        {
            int[] scratch = new int[n * 8];
            var canon = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
            SymmetryHelper.IsIdentityCanonical(canon, scratch).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void IsIdentityCanonical_False_For_NonCanonicalTransform(int n)
    {
        var all = EnumerateAllRawSolutions(n).ToList();
        var first = all.First();
        int[] scratch = new int[n * 8];
        var transforms = SymmetryHelper.GetAllTransforms(first);
        var canon = SymmetryHelper.GetCanonicalForm(first, scratch, null);
        var nonCanon = transforms.First(t => !t.SequenceEqual(canon));
        SymmetryHelper.IsIdentityCanonical(nonCanon, scratch).Should().BeFalse();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void UniqueEnumeration_CountMatchesExpected(int n)
    {
        int cap = int.MaxValue;
        var collected = new List<int[]>();

        // Use the consolidated symmetry-pruned unique counter
        ulong count = Kernel.Solvers.Engines.SymmetryPrunedUniqueCounter.Count(n, cap, rows => collected.Add(rows));

        count.Should().Be(ExpectedSolutionCounts.GetUnique(n));
        collected.Count.Should().BeLessThanOrEqualTo((int)count);

        foreach (var sol in collected)
        {
            int[] scratch = new int[n * 8];
            SymmetryHelper.IsIdentityCanonical(sol, scratch).Should().BeTrue();
        }
    }

    private static IEnumerable<int[]> EnumerateAllRawSolutions(int n)
    {
        int[] rows = new int[n];
        Array.Fill(rows, -1);
        ulong fullMask = n == 64 ? ulong.MaxValue : ((1UL << n) - 1UL);
        return Dfs(0, 0, 0, 0);

        IEnumerable<int[]> Dfs(int col, ulong cols, ulong d1, ulong d2)
        {
            if (col == n)
            {
                var copy = new int[n];
                Array.Copy(rows, copy, n);
                yield return copy;
                yield break;
            }
            ulong avail = ~(cols | d1 | d2) & fullMask;
            for (int r = 0; r < n; r++)
            {
                ulong bit = 1UL << r;
                if ((avail & bit) == 0) continue;
                rows[col] = r;
                foreach (var deeper in Dfs(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1))
                    yield return deeper;
                rows[col] = -1;
            }
        }
    }
}
