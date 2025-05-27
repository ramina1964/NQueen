namespace NQueen.Kernel.Models;

public class Solution
{
    public Solution(int[] queenPositions, int? id = null)
    {
        BoardSize = queenPositions.Length;
        Id = id;
        Name = ToString();
        QueenPositions = queenPositions;
        Positions = SetPositions(QueenPositions);
        Details = GetDetails();
    }

    #region PublicProperties
    public List<Position> Positions { get; set; }

    public int? Id { get; }

    public string Name { get; set; }

    public int[] QueenPositions { get; }

    public string Details { get; set; }

    public sealed override string ToString() => $"No. {Id}";
    #endregion PublicProperties

    #region PrivateMembers
    private int BoardSize { get; }

    private string GetDetails(IndexingType indexingType = IndexingType.ZeroBased)
    {
        const int noOfQueensPerLine = 40;
        // Order by column index ascending
        var columnOrdered = Positions.OrderBy(p => p.ColumnNo).ToList();
        var noOfLines = BoardSize % noOfQueensPerLine == 0 ?
            BoardSize / noOfQueensPerLine :
            BoardSize / noOfQueensPerLine + 1;

        StringBuilder sb = new();
        for (var lineNo = 0; lineNo < noOfLines; lineNo++)
        {
            var maxQueensInLastLine = lineNo < noOfLines - 1 || BoardSize % noOfQueensPerLine == 0 ?
                noOfQueensPerLine :
                Math.Min(BoardSize % noOfQueensPerLine, noOfQueensPerLine);

            for (var posInLine = 0; posInLine < maxQueensInLastLine; posInLine++)
            {
                var posNo = noOfQueensPerLine * lineNo + posInLine;
                if (posNo >= columnOrdered.Count)
                    break;

                if (indexingType == IndexingType.ZeroBased)
                {
                    sb.Append($"({columnOrdered[posNo].ColumnNo,0:N0}, {columnOrdered[posNo].RowNo,0:N0})");
                }
                else
                {
                    sb.Append($"({columnOrdered[posNo].ColumnNo + 1,0:N0}, {columnOrdered[posNo].RowNo + 1,0:N0})");
                }

                if (posNo < BoardSize - 1)
                    sb.Append(", ");
            }

            if (lineNo < noOfLines - 1)
            { sb.AppendLine(); }
        }

        return sb.ToString();
    }

    // Each pair is (column, row) 
    private static List<Position> SetPositions(int[] queenPositions) =>
        queenPositions.Select((rowNo, columnNo) => new Position(rowNo, columnNo)).ToList();

    #endregion PrivateMembers
}
