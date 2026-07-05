namespace NQueen.UnitTests.Tests.Domain;

public class SolutionTests
{
    private static readonly ISolutionFormatter Formatter = new SolutionFormatter();

    [Fact]
    public void ArrayConstructor_PopulatesCoreProperties()
    {
        var solution = new Solution([1, 3, 0, 2], Formatter, id: 7);

        solution.Id.ShouldBe(7);
        solution.BoardSize.ShouldBe(4);
        solution.Name.ShouldBe("Solution No. 07");
        solution.QueenPositions.ShouldBe([1, 3, 0, 2]);
    }

    [Fact]
    public void ArrayConstructor_Positions_MapColumnsAndRows()
    {
        var solution = new Solution([2, 0, 3, 1], Formatter, id: 1);

        solution.Positions.Count.ShouldBe(4);
        solution.Positions[0].RowIndex.ShouldBe(2);
        solution.Positions[3].ColumnIndex.ShouldBe(3);
        solution.Positions[3].RowIndex.ShouldBe(1);
    }

    [Fact]
    public void ArrayConstructor_NullArray_Throws()
    {
        Action act = () => _ = new Solution(null!, Formatter, id: 1);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ArrayConstructor_EmptyArray_Throws()
    {
        Action act = () => _ = new Solution([], Formatter, id: 1);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ArrayConstructor_NegativePosition_Throws()
    {
        Action act = () => _ = new Solution([0, -1, 2], Formatter, id: 1);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void PackedConstructor_UnpacksToOriginalRows()
    {
        int[] rows = [1, 3, 0, 2];
        var packed = Pack(rows);

        var solution = new Solution(packed, rows.Length, Formatter, id: 2);

        solution.BoardSize.ShouldBe(4);
        solution.QueenPositions.ShouldBe(rows);
    }

    [Fact]
    public void PackedConstructor_Positions_RealizeOnAccess()
    {
        int[] rows = [0, 2, 4, 1, 3];
        var solution = new Solution(Pack(rows), rows.Length, Formatter, id: 3);

        var materialized = solution.Positions.Select(p => p.RowIndex).ToList();

        materialized.ShouldBe(rows);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(26)]
    public void PackedConstructor_BoardSizeOutOfRange_Throws(int boardSize)
    {
        Action act = () => _ = new Solution(UInt128.Zero, boardSize, Formatter, id: 1);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Details_UsesFormatter_OneBasedByDefault()
    {
        var solution = new Solution([0, 2, 1, 3], Formatter, id: 1);

        solution.Details.ShouldContain("(1,1)");
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var solution = new Solution([0, 1], Formatter, id: 42);

        solution.ToString().ShouldBe("Solution No. 42");
    }

    [Fact]
    public void Id_WhenNotProvided_UsesGlobalSequence()
    {
        Solution.ResetSequence();

        var first = new Solution([0, 1], Formatter);
        var second = new Solution([0, 1], Formatter);

        first.Id.ShouldBe(1);
        second.Id.ShouldBe(2);
    }

    private static UInt128 Pack(int[] rows)
    {
        UInt128 packed = UInt128.Zero;
        foreach (var r in rows)
        {
            packed <<= 5;
            packed |= (UInt128)(uint)r;
        }

        return packed;
    }
}
