namespace NQueen.UnitTests.Tests.Domain;

public class PositionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 7)]
    [InlineData(12, 5)]
    public void Constructor_ExposesColumnAndRow(int column, int row)
    {
        var position = new Position(column, row);

        position.ColumnIndex.ShouldBe(column);
        position.RowIndex.ShouldBe(row);
    }

    [Fact]
    public void Position_IsValueType_EqualByValue()
    {
        var a = new Position(2, 4);
        var b = new Position(2, 4);

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void Position_DifferentValues_AreNotEqual()
    {
        var a = new Position(2, 4);
        var b = new Position(4, 2);

        a.Equals(b).ShouldBeFalse();
    }
}
