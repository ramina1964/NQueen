namespace NQueen.Domain.Models;

public class Position(int colIndex, int rowIndex)
{
    public int ColumnIndex { get; set; } = colIndex;

    public int RowIndex { get; set; } = rowIndex;
}
