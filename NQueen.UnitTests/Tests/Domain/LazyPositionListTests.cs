namespace NQueen.UnitTests.Tests.Domain;

public class LazyPositionListTests
{
    [Fact]
    public void Count_ReflectsUnderlyingArrayLength()
    {
        var list = new LazyPositionList([0, 2, 1, 3]);

        list.Count.ShouldBe(4);
    }

    [Fact]
    public void Indexer_MapsColumnToIndexAndRowToValue()
    {
        var list = new LazyPositionList([2, 0, 3, 1]);

        list[0].ColumnIndex.ShouldBe(0);
        list[0].RowIndex.ShouldBe(2);
        list[3].ColumnIndex.ShouldBe(3);
        list[3].RowIndex.ShouldBe(1);
    }

    [Fact]
    public void Enumeration_YieldsPositionPerColumnInOrder()
    {
        var list = new LazyPositionList([1, 3, 0, 2]);

        var positions = list.ToList();

        positions.Count.ShouldBe(4);
        positions.Select(p => p.ColumnIndex).ShouldBe([0, 1, 2, 3]);
        positions.Select(p => p.RowIndex).ShouldBe([1, 3, 0, 2]);
    }

    [Fact]
    public void Constructor_NullArray_Throws()
    {
        Action act = () => _ = new LazyPositionList(null!);

        Should.Throw<ArgumentNullException>(act);
    }
}
