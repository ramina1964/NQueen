namespace NQueen.Kernel.Models;

public class Position(int rowNo, int columnNo)
{
    public int RowNo { get; set; } = rowNo;

    public int ColumnNo { get; set; } = columnNo;
}
