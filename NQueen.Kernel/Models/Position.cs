namespace NQueen.Kernel.Models;

public class Position
{
    public Position(byte rowNo, byte columnNo)
    {
        RowNo = rowNo;
        ColumnNo = columnNo;
    }

    public byte RowNo { get; set; }

    public byte ColumnNo { get; set; }
}
