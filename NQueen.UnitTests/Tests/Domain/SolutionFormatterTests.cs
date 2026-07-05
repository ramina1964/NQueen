namespace NQueen.UnitTests.Tests.Domain;

public class SolutionFormatterTests
{
    private readonly SolutionFormatter _formatter = new();

    // ── IndexingType origin ──────────────────────────────────────────────────

    [Theory]
    [InlineData(IndexingType.ZeroBased, "(0,1)", "(1,3)")]  // zero-origin
    [InlineData(IndexingType.OneBased,  "(1,2)", "(2,4)")]  // one-origin (each coordinate +1)
    public void FormatSolutions_FormatsWithExpectedOrigin(IndexingType indexing, string first, string second)
    {
        var positions = new List<Position> { new(0, 1), new(1, 3) };
        var result = _formatter.FormatSolutions(positions, indexing);
        result.ShouldContain(first);
        result.ShouldContain(second);
    }

    // ── Line-wrapping ────────────────────────────────────────────────────────

    [Fact]
    public void FormatSolutions_ExceedsLineLength_WrapsToMultipleLines()
    {
        // noOfQueensPerLine = 2 → 3 positions should produce 2 lines
        var positions = new List<Position> { new(0, 0), new(1, 1), new(2, 2) };
        var result = _formatter.FormatSolutions(positions, IndexingType.ZeroBased, noOfQueensPerLine: 2);
        result.ShouldContain(Environment.NewLine);
    }

    [Fact]
    public void FormatSolutions_ExactLineLength_NoNewline()
    {
        // noOfQueensPerLine = 3, exactly 3 positions → no newline needed
        var positions = new List<Position> { new(0, 0), new(1, 1), new(2, 2) };
        var result = _formatter.FormatSolutions(positions, IndexingType.ZeroBased, noOfQueensPerLine: 3);
        result.ShouldNotContain(Environment.NewLine);
    }

    // ── Ordering ─────────────────────────────────────────────────────────────

    [Fact]
    public void FormatSolutions_UnorderedInput_OutputIsOrderedByColumn()
    {
        var positions = new List<Position> { new(2, 0), new(0, 3), new(1, 1) };
        var result = _formatter.FormatSolutions(positions, IndexingType.ZeroBased);
        var col0Pos = result.IndexOf("(0,", StringComparison.Ordinal);
        var col1Pos = result.IndexOf("(1,", StringComparison.Ordinal);
        var col2Pos = result.IndexOf("(2,", StringComparison.Ordinal);
        col0Pos.ShouldBeLessThan(col1Pos);
        col1Pos.ShouldBeLessThan(col2Pos);
    }

    // ── UpdateSolutionLabel ──────────────────────────────────────────────────

    [Theory]
    [InlineData(SolutionMode.All)]
    [InlineData(SolutionMode.Unique)]
    public void UpdateSolutionLabel_NonSingle_ContainsSolutionsAndMaxDisplayed(SolutionMode mode)
    {
        var label = SolutionFormatter.UpdateSolutionLabel(mode);
        label.ShouldContain("Solutions");
        label.ShouldContain(SimulationSettings.MaxDisplayedCount.ToString());
    }

    [Fact]
    public void UpdateSolutionLabel_Single_ReturnsSolutionExactly() =>
        SolutionFormatter.UpdateSolutionLabel(SolutionMode.Single).ShouldBe("Solution");
}
