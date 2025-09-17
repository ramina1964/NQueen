namespace NQueen.Domain.Models;

public readonly struct Position(int columnIndex, int rowIndex)
{
    public int ColumnIndex => columnIndex;

    public int RowIndex => rowIndex;
}