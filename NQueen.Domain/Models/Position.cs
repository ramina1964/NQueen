namespace NQueen.Domain.Models;

public class Position(int rowIndex, int colIndex)
{
    public int RowNo { get; set; } = rowIndex;

    public int ColumnNo { get; set; } = colIndex;
}
