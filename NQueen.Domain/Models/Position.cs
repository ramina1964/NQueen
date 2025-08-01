namespace NQueen.Domain.Models;

// Todo: Change the order of parameters and properties.
public class Position(int rowIndex, int colIndex)
{
    public int RowIndex { get; set; } = rowIndex;

    public int ColumnNo { get; set; } = colIndex;
}
