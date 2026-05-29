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
}
