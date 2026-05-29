namespace NQueen.UnitTests.Tests.Domain;

public class DomainUtilityTests
{
    // ── ValidationHelper ────────────────────────────────────────────────────

    [Fact]
    public void ValidationHelper_AllPositive_ReturnsTrue()
    {
        Span<int> positions = [0, 1, 2, 3];
        ValidationHelper.AreAllPositionsValid(positions).Should().BeTrue();
    }

    [Fact]
    public void ValidationHelper_ContainsNegative_ReturnsFalse()
    {
        Span<int> positions = [0, -1, 2];
        ValidationHelper.AreAllPositionsValid(positions).Should().BeFalse();
    }

    [Fact]
    public void ValidationHelper_Empty_ReturnsTrue()
    {
        Span<int> positions = [];
        ValidationHelper.AreAllPositionsValid(positions).Should().BeTrue();
    }

    // ── IntArrayStructuralComparer ───────────────────────────────────────────

    [Fact]
    public void IntArrayStructuralComparer_SameReference_ReturnsTrue()
    {
        var arr = new[] { 1, 2, 3 };
        IntArrayStructuralComparer.Instance.Equals(arr, arr).Should().BeTrue();
    }

    [Fact]
    public void IntArrayStructuralComparer_EqualContent_ReturnsTrue()
    {
        IntArrayStructuralComparer.Instance.Equals([1, 2, 3], [1, 2, 3]).Should().BeTrue();
    }

    [Fact]
    public void IntArrayStructuralComparer_DifferentContent_ReturnsFalse()
    {
        IntArrayStructuralComparer.Instance.Equals([1, 2, 3], [1, 2, 4]).Should().BeFalse();
    }

    [Fact]
    public void IntArrayStructuralComparer_DifferentLength_ReturnsFalse()
    {
        IntArrayStructuralComparer.Instance.Equals([1, 2], [1, 2, 3]).Should().BeFalse();
    }

    [Fact]
    public void IntArrayStructuralComparer_NullInputs_ReturnsFalse()
    {
        IntArrayStructuralComparer.Instance.Equals(null, [1]).Should().BeFalse();
        IntArrayStructuralComparer.Instance.Equals([1], null).Should().BeFalse();
    }

    [Fact]
    public void IntArrayStructuralComparer_GetHashCode_SameForEqualArrays()
    {
        var h1 = IntArrayStructuralComparer.Instance.GetHashCode([1, 2, 3]);
        var h2 = IntArrayStructuralComparer.Instance.GetHashCode([1, 2, 3]);
        h1.Should().Be(h2);
    }

    [Fact]
    public void IntArrayStructuralComparer_GetHashCode_NullThrows()
    {
        var act = () => IntArrayStructuralComparer.Instance.GetHashCode(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── MemoryIntArrayComparer (Compare) ────────────────────────────────────

    [Fact]
    public void MemoryIntArrayComparer_Compare_Equal_ReturnsZero()
    {
        var c = MemoryIntArrayComparer.Instance;
        c.Compare(new Memory<int>([1, 2, 3]), new Memory<int>([1, 2, 3])).Should().Be(0);
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_LessThan_ReturnsNegative()
    {
        var c = MemoryIntArrayComparer.Instance;
        c.Compare(new Memory<int>([1, 2, 2]), new Memory<int>([1, 2, 3])).Should().BeNegative();
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_GreaterThan_ReturnsPositive()
    {
        var c = MemoryIntArrayComparer.Instance;
        c.Compare(new Memory<int>([1, 2, 4]), new Memory<int>([1, 2, 3])).Should().BePositive();
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_ShorterIsLess()
    {
        var c = MemoryIntArrayComparer.Instance;
        c.Compare(new Memory<int>([1, 2]), new Memory<int>([1, 2, 3])).Should().BeNegative();
    }

    [Fact]
    public void MemoryIntArrayComparer_GetHashCode_EmptyArray_ReturnsZero()
    {
        MemoryIntArrayComparer.Instance.GetHashCode(Memory<int>.Empty).Should().Be(0);
    }

    // ── ErrorMessages ────────────────────────────────────────────────────────

    [Fact]
    public void ErrorMessages_Constants_NotNullOrEmpty()
    {
        ErrorMessages.InvalidIntegerError.Should().NotBeNullOrEmpty();
        ErrorMessages.NoSolutionMessage.Should().NotBeNullOrEmpty();
        ErrorMessages.ValueNullOrWhiteSpaceMsg.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ErrorMessages_Properties_ContainBoardSizeValues()
    {
        ErrorMessages.SizeTooSmallMsg.Should().Contain(BoardSettings.MinSize.ToString());
        ErrorMessages.SizeTooLargeForSingle.Should().Contain(BoardSettings.MaxSizeForSingle.ToString());
        ErrorMessages.SizeTooLargeForUnique.Should().Contain(BoardSettings.MaxSizeForUnique.ToString());
        ErrorMessages.SizeTooLargeForAll.Should().Contain(BoardSettings.MaxSizeForAll.ToString());
        ErrorMessages.VisualizeSizeTooLarge.Should().Contain(SimulationSettings.MaxVisualizeBoardSize.ToString());
    }

    [Fact]
    public void ErrorMessages_GetTimeoutMessage_ContainsSeconds()
    {
        var msg = ErrorMessages.GetTimeoutMessage(TimeSpan.FromSeconds(30));
        msg.Should().Contain("30");
    }

    // ── ExpectedSolutionCounts ───────────────────────────────────────────────

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(4, 2UL)]
    [InlineData(8, 92UL)]
    public void ExpectedSolutionCounts_GetAllFast_ReturnsKnownValues(int n, ulong expected)
    {
        ExpectedSolutionCounts.GetAllFast(n).Should().Be(expected);
    }

    [Fact]
    public void ExpectedSolutionCounts_GetAllFast_OutOfRange_ReturnsZero()
    {
        ExpectedSolutionCounts.GetAllFast(0).Should().Be(0UL);
        ExpectedSolutionCounts.GetAllFast(999).Should().Be(0UL);
    }

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(8, 12UL)]
    public void ExpectedSolutionCounts_GetUniqueFast_ReturnsKnownValues(int n, ulong expected)
    {
        ExpectedSolutionCounts.GetUniqueFast(n).Should().Be(expected);
    }

    [Fact]
    public void ExpectedSolutionCounts_TryGetAll_KnownSize_ReturnsTrue()
    {
        ExpectedSolutionCounts.TryGetAll(8, out var count).Should().BeTrue();
        count.Should().Be(92UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_TryGetUnique_KnownSize_ReturnsTrue()
    {
        ExpectedSolutionCounts.TryGetUnique(8, out var count).Should().BeTrue();
        count.Should().Be(12UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_AllSolutions_DictionaryNotEmpty()
    {
        ExpectedSolutionCounts.AllSolutions.Should().NotBeEmpty();
    }

    [Fact]
    public void ExpectedSolutionCounts_UniqueSolutions_DictionaryNotEmpty()
    {
        ExpectedSolutionCounts.UniqueSolutions.Should().NotBeEmpty();
    }

    // ── DefaultSolutionFormatter (merged into SolutionFormatter) ─────────────

    [Fact]
    public void DefaultSolutionFormatter_FormatSolutions_ProducesCommaSeparatedPositions()
    {
        var formatter = new SolutionFormatter();
        var positions = new List<Position> { new(0, 1), new(1, 3) };

        var result = formatter.FormatSolutions(positions);

        result.Should().Contain("(1,2)").And.Contain("(2,4)");
    }

    [Fact]
    public void DefaultSolutionFormatter_FormatSolutions_EmptyList_ReturnsEmpty()
    {
        var formatter = new SolutionFormatter();
        formatter.FormatSolutions([]).Should().BeEmpty();
    }

    // ── SolutionFormatter ────────────────────────────────────────────────────

    [Fact]
    public void SolutionFormatter_FormatSolutions_OrdersByColumn()
    {
        var formatter = new SolutionFormatter();
        var positions = new List<Position> { new(2, 0), new(0, 3), new(1, 1) };

        var result = formatter.FormatSolutions(positions);

        result.Should().NotBeNullOrEmpty();
        var first = result.IndexOf("(0,", StringComparison.Ordinal);
        var second = result.IndexOf("(1,", StringComparison.Ordinal);
        first.Should().BeLessThan(second);
    }

    [Fact]
    public void SolutionFormatter_UpdateSolutionLabel_Single_ReturnsSolution()
    {
        SolutionFormatter.UpdateSolutionLabel(SolutionMode.Single).Should().Be("Solution");
    }

    [Fact]
    public void SolutionFormatter_UpdateSolutionLabel_Unique_ContainsMaxDisplayed()
    {
        SolutionFormatter.UpdateSolutionLabel(SolutionMode.Unique).Should().Contain("Solutions");
    }

    // ── MenuState ────────────────────────────────────────────────────────────

    [Fact]
    public void MenuState_DefaultValues_AreFalseAndZero()
    {
        var state = new MenuState();
        state.ExitRequested.Should().BeFalse();
        state.BlankInputCount.Should().Be(0);
    }

    [Fact]
    public void MenuState_SetProperties_ReflectChanges()
    {
        var state = new MenuState { ExitRequested = true, BlankInputCount = 3 };
        state.ExitRequested.Should().BeTrue();
        state.BlankInputCount.Should().Be(3);
    }

    [Fact]
    public void MenuState_RecordEquality_Works()
    {
        var a = new MenuState { ExitRequested = false, BlankInputCount = 1 };
        var b = new MenuState { ExitRequested = false, BlankInputCount = 1 };
        a.Should().Be(b);
    }

    // ── SimulationSettings ───────────────────────────────────────────────────

    [Fact]
    public void SimulationSettings_Constants_HaveExpectedValues()
    {
        SimulationSettings.MaxDisplayedCount.Should().Be(5);
        SimulationSettings.DefaultSolutionMode.Should().Be(SolutionMode.Unique);
        SimulationSettings.DefaultDisplayMode.Should().Be(DisplayMode.Hide);
        SimulationSettings.MaxVisualizeBoardSize.Should().Be(10);
    }

    // ── ExpectedSolutionCounts ───────────────────────────────────────────────

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(4, 2UL)]
    [InlineData(5, 10UL)]
    [InlineData(8, 92UL)]
    public void ExpectedSolutionCounts_GetAllFast_ReturnsKnownValues(int n, ulong expected) =>
        ExpectedSolutionCounts.GetAllFast(n).Should().Be(expected);

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(5, 2UL)]
    [InlineData(8, 12UL)]
    public void ExpectedSolutionCounts_GetUniqueFast_ReturnsKnownValues(int n, ulong expected) =>
        ExpectedSolutionCounts.GetUniqueFast(n).Should().Be(expected);

    [Fact]
    public void ExpectedSolutionCounts_GetAllFast_OutOfRange_ReturnsZero()
    {
        ExpectedSolutionCounts.GetAllFast(0).Should().Be(0UL);
        ExpectedSolutionCounts.GetAllFast(100).Should().Be(0UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_GetUniqueFast_OutOfRange_ReturnsZero()
    {
        ExpectedSolutionCounts.GetUniqueFast(0).Should().Be(0UL);
        ExpectedSolutionCounts.GetUniqueFast(100).Should().Be(0UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_TryGetAll_KnownN_ReturnsTrueAndCorrectCount()
    {
        ExpectedSolutionCounts.TryGetAll(8, out var count).Should().BeTrue();
        count.Should().Be(92UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_TryGetAll_UnknownN_ReturnsFalse() =>
        ExpectedSolutionCounts.TryGetAll(0, out _).Should().BeFalse();

    [Fact]
    public void ExpectedSolutionCounts_TryGetUnique_KnownN_ReturnsTrueAndCorrectCount()
    {
        ExpectedSolutionCounts.TryGetUnique(8, out var count).Should().BeTrue();
        count.Should().Be(12UL);
    }

    [Fact]
    public void ExpectedSolutionCounts_AllSolutionsSpan_ContainsKnownEntry() =>
        ExpectedSolutionCounts.AllSolutionsSpan[8].Should().Be(92UL);

    [Fact]
    public void ExpectedSolutionCounts_UniqueSolutionsSpan_ContainsKnownEntry() =>
        ExpectedSolutionCounts.UniqueSolutionsSpan[8].Should().Be(12UL);

    [Fact]
    public void ExpectedSolutionCounts_AllSolutionsDictionary_ContainsN8() =>
        ExpectedSolutionCounts.AllSolutions.Should().ContainKey(8);

    [Fact]
    public void ExpectedSolutionCounts_UniqueSolutionsDictionary_ContainsN8() =>
        ExpectedSolutionCounts.UniqueSolutions.Should().ContainKey(8);

    // ── MemoryIntArrayComparer.Compare ───────────────────────────────────────

    [Fact]
    public void MemoryIntArrayComparer_Compare_EqualArrays_ReturnsZero()
    {
        Memory<int> a = new([1, 2, 3]);
        Memory<int> b = new([1, 2, 3]);
        MemoryIntArrayComparer.Instance.Compare(a, b).Should().Be(0);
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_LessThan_ReturnsNegative()
    {
        Memory<int> a = new([1, 2, 3]);
        Memory<int> b = new([1, 2, 4]);
        MemoryIntArrayComparer.Instance.Compare(a, b).Should().BeNegative();
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_GreaterThan_ReturnsPositive()
    {
        Memory<int> a = new([1, 2, 4]);
        Memory<int> b = new([1, 2, 3]);
        MemoryIntArrayComparer.Instance.Compare(a, b).Should().BePositive();
    }

    [Fact]
    public void MemoryIntArrayComparer_Compare_ShorterLengthFirst_ReturnsNegative()
    {
        Memory<int> a = new([1, 2]);
        Memory<int> b = new([1, 2, 3]);
        MemoryIntArrayComparer.Instance.Compare(a, b).Should().BeNegative();
    }

    [Fact]
    public void MemoryIntArrayComparer_GetHashCode_Empty_ReturnsZero()
    {
        Memory<int> empty = new([]);
        MemoryIntArrayComparer.Instance.GetHashCode(empty).Should().Be(0);
    }
}
