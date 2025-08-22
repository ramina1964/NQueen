namespace NQueen.Domain.Models;

public readonly struct Position(int colIndex, int rowIndex)
{
    public int ColumnIndex => colIndex;

    public int RowIndex => rowIndex;
}