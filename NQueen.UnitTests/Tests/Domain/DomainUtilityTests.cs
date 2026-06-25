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

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, true)]   // equal content
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 4 }, false)]  // different content
    [InlineData(new[] { 1, 2 }, new[] { 1, 2, 3 }, false)]     // different length
    public void IntArrayStructuralComparer_Equals_MatchesContent(int[] left, int[] right, bool expected)
    {
        IntArrayStructuralComparer.Instance.Equals(left, right).Should().Be(expected);
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

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, 0)]   // equal → zero
    [InlineData(new[] { 1, 2, 2 }, new[] { 1, 2, 3 }, -1)]  // less than → negative
    [InlineData(new[] { 1, 2, 4 }, new[] { 1, 2, 3 }, 1)]   // greater than → positive
    [InlineData(new[] { 1, 2 }, new[] { 1, 2, 3 }, -1)]     // shorter is less → negative
    public void MemoryIntArrayComparer_Compare_ReturnsExpectedSign(int[] left, int[] right, int expectedSign)
    {
        var result = MemoryIntArrayComparer.Instance.Compare(new Memory<int>(left), new Memory<int>(right));
        Math.Sign(result).Should().Be(expectedSign);
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
        ErrorMessages.NoSolutionMsg.Should().NotBeNullOrEmpty();
        ErrorMessages.ValueNullOrWhiteSpaceMsg.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ErrorMessages_Properties_ContainBoardSizeValues()
    {
        ErrorMessages.OutOfRangeMsg.Should().Contain(BoardSettings.MinSize.ToString());
        ErrorMessages.OutOfRangeSingle.Should().Contain(BoardSettings.MaxSizeForSingle.ToString());
        ErrorMessages.OutOfRangeUnique.Should().Contain(BoardSettings.MaxSizeForUnique.ToString());
        ErrorMessages.OutOfRangeAll.Should().Contain(BoardSettings.MaxSizeForAll.ToString());
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
    [InlineData(5, 10UL)]
    [InlineData(7, 40UL)]
    [InlineData(8, 92UL)]
    [InlineData(0, 0UL)]    // out of range → zero
    [InlineData(999, 0UL)]  // out of range → zero
    public void ExpectedSolutionCounts_GetAllFast_ReturnsKnownValues(int n, ulong expected)
    {
        ExpectedSolutionCounts.GetAllFast(n).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1UL)]
    [InlineData(5, 2UL)]
    [InlineData(7, 6UL)]
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

    // ── ExpectedSolutionCounts — additional coverage ─────────────────────────

    [Fact]
    public void ExpectedSolutionCounts_TryGetAll_UnknownN_ReturnsFalse() =>
        ExpectedSolutionCounts.TryGetAll(0, out _).Should().BeFalse();

    [Fact]
    public void ExpectedSolutionCounts_AllSolutionsSpan_ContainsKnownEntry() =>
        ExpectedSolutionCounts.AllSolutionsSpan[8].Should().Be(92UL);

    [Fact]
    public void ExpectedSolutionCounts_UniqueSolutionsSpan_ContainsKnownEntry() =>
        ExpectedSolutionCounts.UniqueSolutionsSpan[8].Should().Be(12UL);
}
