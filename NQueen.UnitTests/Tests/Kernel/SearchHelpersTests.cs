namespace NQueen.UnitTests.Tests.Kernel;

public class SearchHelpersTests
{
    // ── IsOddCenterFirstRow ──────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 2,  true)]   // odd N=5, center row = 2
    [InlineData(5, 0,  false)]  // odd N=5, not center
    [InlineData(4, 2,  false)]  // even N=4 → always false
    [InlineData(7, 3,  true)]   // odd N=7, center row = 3
    [InlineData(7, 2,  false)]  // odd N=7, not center
    public void IsOddCenterFirstRow_ReturnsExpected(int n, int r, bool expected) =>
        SearchHelpers.IsOddCenterFirstRow(n, r).Should().Be(expected);

    // ── PackIdentityKey ──────────────────────────────────────────────────────

    [Fact]
    public void PackIdentityKey_CanonicalSolution_ReturnsDeterministicKey()
    {
        int[] sol = [1, 3, 0, 2]; // canonical form of N=4 solution
        int[] scratch = new int[4 * 8];
        var canon = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
        var key1 = SearchHelpers.PackIdentityKey(canon, scratch);
        var key2 = SearchHelpers.PackIdentityKey(canon, scratch);
        key1.Should().Be(key2);
    }

    [Fact]
    public void PackIdentityKey_NonCanonicalSolution_ReturnsCanonicalKey()
    {
        // A non-canonical transform should produce the same key as the canonical form
        int[] sol = [0, 2, 4, 1, 3];
        int[] scratch = new int[5 * 8];
        var canon = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
        var keyFromSol   = SearchHelpers.PackIdentityKey(sol, scratch);
        var keyFromCanon = SearchHelpers.PackIdentityKey(canon, scratch);
        keyFromSol.Should().Be(keyFromCanon);
    }

    // ── PackIdentityKeyAndRows ───────────────────────────────────────────────

    [Fact]
    public void PackIdentityKeyAndRows_CanonicalSolution_KeyMatchesPackRows()
    {
        int[] sol = [1, 3, 0, 2];
        int[] scratch = new int[4 * 8];
        var canon = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
        var (key, rows) = SearchHelpers.PackIdentityKeyAndRows(canon, scratch, canon.Length);
        var expected = SymmetryHelper.PackRows(rows);
        key.Should().Be(expected);
        rows.Should().HaveCount(canon.Length);
    }

    [Fact]
    public void PackIdentityKeyAndRows_NonCanonicalSolution_ReturnsCanonicalRows()
    {
        int[] sol = [0, 2, 4, 1, 3];
        int[] scratch = new int[5 * 8];
        var canon = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
        var (_, rows) = SearchHelpers.PackIdentityKeyAndRows(sol, scratch, sol.Length);
        rows.Should().BeEquivalentTo(canon, options => options.WithStrictOrdering());
    }

    // ── ShouldPrunePrefixIncremental ─────────────────────────────────────────

    [Fact]
    public void ShouldPrunePrefixIncremental_BothDisabled_ReturnsFalse()
    {
        int[] rows = [0, 3, 1, 2];
        bool reflEq = true, minEq = true;
        SearchHelpers.ShouldPrunePrefixIncremental(
            rows, 1, 4, false, false, ref reflEq, ref minEq)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldPrunePrefixIncremental_ReflectionPrune_DetectsLargerReflection()
    {
        // N=4: at depth=1, row=3 → reflected=0 → row(3) > reflected(0) → prune
        int[] rows = [-1, 3, -1, -1];
        bool reflEq = true, minEq = false;
        var result = SearchHelpers.ShouldPrunePrefixIncremental(
            rows, 1, 4, reflectionEnabled: true, minimalityEnabled: false,
            ref reflEq, ref minEq);
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldPrunePrefixIncremental_ReflectionPrune_SmallerReflection_ClearsFlag()
    {
        // N=4: at depth=1, row=0 → reflected=3 → row(0) < reflected(3) → not pruned, flag cleared
        int[] rows = [-1, 0, -1, -1];
        bool reflEq = true, minEq = false;
        SearchHelpers.ShouldPrunePrefixIncremental(
            rows, 1, 4, reflectionEnabled: true, minimalityEnabled: false,
            ref reflEq, ref minEq);
        reflEq.Should().BeFalse();
    }

    [Fact]
    public void ShouldPrunePrefixIncremental_MinimalityPrune_PrunesWhenFirstRowTransformSmaller()
    {
        // N=4: rows[0]=3, at depth=1, rows[1]=0 → transformed = 4-1-0 = 3
        // first(3) > transformed(3)? No, equal → not pruned but flag stays
        // Let's use rows[0]=3, rows[1]=1 → transformed=2 → first(3)>transformed(2) → prune
        int[] rows = [3, 1, -1, -1];
        bool reflEq = false, minEq = true;
        var result = SearchHelpers.ShouldPrunePrefixIncremental(
            rows, 1, 4, reflectionEnabled: false, minimalityEnabled: true,
            ref reflEq, ref minEq);
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldPrunePrefixIncremental_NegativeRowValue_ReturnsFalse()
    {
        int[] rows = [-1, -1, -1, -1];
        bool reflEq = true, minEq = true;
        SearchHelpers.ShouldPrunePrefixIncremental(
            rows, 0, 4, true, true, ref reflEq, ref minEq)
            .Should().BeFalse();
    }

    // ── ShouldPrunePrefixFull (reflection-only, stateless) ───────────────────

    [Theory]
    // With reflection off there is no sound forward-prefix prune, so never prune.
    [InlineData(new[] { 7, 1, -1, -1, -1, -1, -1, -1 }, 1, 8, false, false)]
    // N=8: row 7 mirrors to 0, so 7 > 0 → the horizontal reflection is lexicographically
    // smaller, so this prefix can never be the canonical representative → prune.
    [InlineData(new[] { 7, -1, -1, -1, -1, -1, -1, -1 }, 0, 8, true, true)]
    // N=8: row 2 mirrors to 5, so 2 < 5 → identity already wins the reflection comparison
    // at column 0; the scan breaks and does not prune.
    [InlineData(new[] { 2, -1, -1, -1, -1, -1, -1, -1 }, 0, 8, true, false)]
    // Negative (unfixed) row value → never prune.
    [InlineData(new[] { -1, -1, -1, -1 }, 0, 4, true, false)]
    public void ShouldPrunePrefixFull_ReturnsExpected(int[] rows, int depth, int n, bool reflectionEnabled, bool expected)
    {
        SearchHelpers.ShouldPrunePrefixFull(rows, depth, n, reflectionEnabled).Should().Be(expected);
    }

    [Fact]
    public void ShouldPrunePrefixFull_DoesNotApplyUnsoundRotate180MinimalityPrune()
    {
        // Regression guard for the N>=16 unique under-count (692 857 vs 1 846 955 at N=16).
        // rows=[2,6] at N=8: reflection at col 0 (2 < 5) clears the comparison, so reflection
        // never prunes. The old rotate-180 "minimality" prune WOULD have fired here
        // (rows[0]=2 > N-1-rows[1] = 7-6 = 1), incorrectly discarding a branch that can still
        // reach a canonical solution. ShouldPrunePrefixFull must NOT prune.
        int[] rows = [2, 6, -1, -1, -1, -1, -1, -1];
        SearchHelpers.ShouldPrunePrefixFull(rows, 1, 8, reflectionEnabled: true)
            .Should().BeFalse("rotate-180 minimality is unsound as a forward-prefix prune and must not be applied");
    }
}
