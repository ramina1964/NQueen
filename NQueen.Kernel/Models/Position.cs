namespace NQueen.Kernel.Models;

public class Position(byte rowNo, byte columnNo)
{
    public byte RowNo { get; set; } = rowNo;

    public byte ColumnNo { get; set; } = columnNo;
}
