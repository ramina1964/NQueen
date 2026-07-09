namespace NQueen.UnitTests.Tests.Symmetry;

[Trait("Category", "Symmetry")]
[Trait("Behavior", "UniqueCounter")]
public class SymmetryPrunedUniqueCounterTests
{
    // ── Basic counts (N ≤ 8, fast) ───────────────────────────────────────────

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(4, 1UL)]
    [InlineData(5, 2UL)]
    [InlineData(6, 1UL)]
    [InlineData(7, 6UL)]
    [InlineData(8, 12UL)]
    public void Count_WithoutPruning_MatchesExpectedUniqueSolutions(int n, ulong expected) =>
        SymmetryPrunedUniqueCounter.Count(n, cap: 0).ShouldBe(expected);

    [Fact]
    public void Count_ZeroBoardSize_ReturnsZero() =>
        SymmetryPrunedUniqueCounter.Count(0, cap: 0).ShouldBe(0UL);

    [Fact]
    public void Count_NegativeBoardSize_ReturnsZero() =>
        SymmetryPrunedUniqueCounter.Count(-1, cap: 0).ShouldBe(0UL);

    // ── Materialization callback ─────────────────────────────────────────────

    [Fact]
    public void Count_WithCallback_MaterializesUpToCap()
    {
        var collected = new System.Collections.Concurrent.ConcurrentBag<int[]>();
        var count = SymmetryPrunedUniqueCounter.Count(6, cap: 1, onMaterialized: rows => collected.Add(rows));
        count.ShouldBe(1UL);
        collected.Count().ShouldBe(1);
        collected.First().Count().ShouldBe(6);
    }

    [Fact]
    public void Count_WithCallback_N5_CollectsAllUniqueSolutions()
    {
        var collected = new List<int[]>();
        var count = SymmetryPrunedUniqueCounter.Count(5, cap: 10, onMaterialized: rows => collected.Add(rows));
        count.ShouldBe(2UL);
        collected.Count().ShouldBe(2);
    }

    // ── Pruning flags ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 2UL)]
    [InlineData(6, 1UL)]
    public void Count_WithReflectionPruning_MatchesExpectedCount(int n, ulong expected) =>
        SymmetryPrunedUniqueCounter.Count(n, cap: 0, reflectionPruning: true).ShouldBe(expected);

    [Theory]
    [InlineData(5, 2UL)]
    [InlineData(6, 1UL)]
    public void Count_WithPrefixMinimality_MatchesExpectedCount(int n, ulong expected) =>
        SymmetryPrunedUniqueCounter.Count(n, cap: 0, prefixMinimality: true).ShouldBe(expected);

    [Theory]
    [InlineData(5, 2UL)]
    public void Count_BothPruningFlags_MatchesExpectedCount(int n, ulong expected) =>
        SymmetryPrunedUniqueCounter.Count(n, cap: 0, prefixMinimality: true, reflectionPruning: true)
            .ShouldBe(expected);

    // ── Solutions are valid placements ───────────────────────────────────────

    [Fact]
    public void Count_N6_MaterializedSolutions_AreValidPlacements()
    {
        var solutions = new List<int[]>();
        SymmetryPrunedUniqueCounter.Count(6, cap: 1, onMaterialized: rows => solutions.Add(rows));
        solutions.ShouldNotBeEmpty();
        foreach (var sol in solutions)
        {
            sol.Count().ShouldBe(6);
            sol.Distinct().Count().ShouldBe(6, "no two queens in the same row");
        }
    }
}
