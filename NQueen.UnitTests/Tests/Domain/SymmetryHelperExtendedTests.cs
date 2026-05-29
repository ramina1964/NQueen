namespace NQueen.UnitTests.Tests.Domain;

public class SymmetryHelperExtendedTests
{
    // ── ApplyAdvancedSymmetryPruning ─────────────────────────────────────────

    [Fact]
    public void ApplyAdvancedSymmetryPruning_BoardSizeOne_ReturnsMaskUnchanged()
    {
        ulong mask = 0b1111UL;
        SymmetryHelper.ApplyAdvancedSymmetryPruning(1, 0, [0], mask).Should().Be(mask);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column0_EvenBoard_CutsToHalf()
    {
        // N=8: maxRow=4, so bits 0..3 only
        ulong fullMask = 0xFFUL;
        var result = SymmetryHelper.ApplyAdvancedSymmetryPruning(8, 0, new int[8], fullMask);
        result.Should().Be(0b00001111UL);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column0_OddBoard_CutsToHalfPlusOne()
    {
        // N=5: maxRow=(5+1)/2=3, bits 0..2
        ulong fullMask = 0b11111UL;
        var result = SymmetryHelper.ApplyAdvancedSymmetryPruning(5, 0, new int[5], fullMask);
        result.Should().Be(0b00111UL);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column1_NormalFirstRow_ClearsLowerBits()
    {
        // N=8, firstRow=2 → minRow=3 → clear bits 0,1,2
        var queenRows = new int[8];
        queenRows[0] = 2;
        ulong fullMask = 0xFFUL;
        var result = SymmetryHelper.ApplyAdvancedSymmetryPruning(8, 1, queenRows, fullMask);
        (result & 0b111UL).Should().Be(0UL); // bits 0,1,2 cleared
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column1_FirstRowAtLastPosition_ReturnsZero()
    {
        // N=4, firstRow=3 → minRow=4 >= boardSize → availMask=0
        var queenRows = new int[4];
        queenRows[0] = 3;
        var result = SymmetryHelper.ApplyAdvancedSymmetryPruning(4, 1, queenRows, 0xFUL);
        result.Should().Be(0UL);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column1_OddBoardCenterRow_NoRestriction()
    {
        // N=5, firstRow=2 (= boardSize/2 = center) → no restriction applied
        var queenRows = new int[5];
        queenRows[0] = 2;
        ulong fullMask = 0b11111UL;
        var result = SymmetryHelper.ApplyAdvancedSymmetryPruning(5, 1, queenRows, fullMask);
        result.Should().Be(fullMask);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_Column2_ReturnsMaskUnchanged()
    {
        ulong mask = 0b11111UL;
        SymmetryHelper.ApplyAdvancedSymmetryPruning(5, 2, new int[5], mask).Should().Be(mask);
    }

    [Fact]
    public void ApplyAdvancedSymmetryPruning_NullQueenRows_Throws() =>
        FluentActions.Invoking(() => SymmetryHelper.ApplyAdvancedSymmetryPruning(4, 0, null!, 0xFUL))
            .Should().Throw<ArgumentNullException>();

    // ── AddIfUnique / AddIfUniquePacked ──────────────────────────────────────

    [Fact]
    public void AddIfUnique_NewSolution_ReturnsTrue()
    {
        var keys = new HashSet<UInt128>();
        int[] scratch = new int[5 * 8];
        SymmetryHelper.AddIfUnique([0, 2, 4, 1, 3], keys, scratch).Should().BeTrue();
    }

    [Fact]
    public void AddIfUnique_DuplicateSolution_ReturnsFalse()
    {
        var keys = new HashSet<UInt128>();
        int[] scratch = new int[5 * 8];
        int[] sol = [0, 2, 4, 1, 3];
        SymmetryHelper.AddIfUnique(sol, keys, scratch);
        SymmetryHelper.AddIfUnique(sol, keys, scratch).Should().BeFalse();
    }

    [Fact]
    public void AddIfUniquePacked_NewSolution_ReturnsKeyAndCanonicalCopy()
    {
        var keys = new HashSet<UInt128>();
        int[] scratch = new int[5 * 8];
        bool added = SymmetryHelper.AddIfUniquePacked([0, 2, 4, 1, 3], keys, scratch, out var key, out var copy);
        added.Should().BeTrue();
        key.Should().NotBe(UInt128.Zero);
        copy.Should().HaveCount(5);
    }

    [Fact]
    public void AddIfUniquePackedReuseBuffer_NewSolution_ReturnsTrue()
    {
        var keys = new HashSet<UInt128>();
        int[] scratch = new int[5 * 8];
        int[] buf = new int[5];
        bool added = SymmetryHelper.AddIfUniquePackedReuseBuffer(
            [0, 2, 4, 1, 3], keys, scratch, buf, out var key, out var copy);
        added.Should().BeTrue();
        key.Should().NotBe(UInt128.Zero);
        copy.Should().HaveCount(5);
    }

    // ── GetCanonicalForm (single-argument overload) ──────────────────────────

    [Fact]
    public void GetCanonicalForm_SingleArg_EmptyArray_ReturnsEmpty() =>
        SymmetryHelper.GetCanonicalForm([]).Should().BeEmpty();

    [Fact]
    public void GetCanonicalForm_SingleArg_ReturnsSameAsScratchOverload()
    {
        int[] sol = [1, 3, 0, 2];
        int[] scratch = new int[4 * 8];
        var via1 = SymmetryHelper.GetCanonicalForm(sol);
        var via2 = SymmetryHelper.GetCanonicalForm(sol, scratch, null);
        via1.Should().BeEquivalentTo(via2);
    }

    [Fact]
    public void GetCanonicalForm_SingleArg_NullThrows() =>
        FluentActions.Invoking(() => SymmetryHelper.GetCanonicalForm(null!))
            .Should().Throw<ArgumentNullException>();

    // ── PackRows / PackCanonical ─────────────────────────────────────────────

    [Fact]
    public void PackRows_SameSequence_ProducesSameKey()
    {
        ReadOnlySpan<int> rows = [0, 2, 4, 1, 3];
        var k1 = SymmetryHelper.PackRows(rows);
        var k2 = SymmetryHelper.PackRows(rows);
        k1.Should().Be(k2);
    }

    [Fact]
    public void PackRows_DifferentSequences_ProduceDifferentKeys()
    {
        var k1 = SymmetryHelper.PackRows([0, 2, 4, 1, 3]);
        var k2 = SymmetryHelper.PackRows([3, 1, 4, 2, 0]);
        k1.Should().NotBe(k2);
    }

    [Fact]
    public void PackCanonical_SubsetSlice_MatchesPackRows()
    {
        int[] rows = [0, 2, 4, 1, 3];
        var viaPackRows = SymmetryHelper.PackRows(rows);
        var viaPackCanonical = SymmetryHelper.PackCanonical(rows, rows.Length);
        viaPackCanonical.Should().Be(viaPackRows);
    }

    // ── MaxRowExclusiveForColumn / GetScratchBufferSize ──────────────────────

    [Theory]
    [InlineData(8, 0, 4)]
    [InlineData(8, 1, 8)]
    [InlineData(5, 0, 3)]
    public void MaxRowExclusiveForColumn_ReturnsExpected(int boardSize, int col, int expected) =>
        SymmetryHelper.MaxRowExclusiveForColumn(boardSize, col, new int[boardSize]).Should().Be(expected);

    [Theory]
    [InlineData(4,  32)]
    [InlineData(8,  64)]
    [InlineData(16, 128)]
    public void GetScratchBufferSize_ReturnsEightTimesN(int n, int expected) =>
        SymmetryHelper.GetScratchBufferSize(n).Should().Be(expected);

    // ── GetCanonicalKey ──────────────────────────────────────────────────────

    [Fact]
    public void GetCanonicalKey_ReturnsKeyMatchingGetCanonicalForm()
    {
        int[] sol = [1, 3, 0, 2];
        int[] scratch = new int[4 * 8];
        var key = SymmetryHelper.GetCanonicalKey(sol, scratch, out var canonical);
        var expectedKey = SymmetryHelper.PackRows(canonical);
        key.Should().Be(expectedKey);
    }
}
