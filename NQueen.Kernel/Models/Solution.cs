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
    public List<Position> Positions;

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
                if (indexingType == IndexingType.ZeroBased)
                {
                    sb.Append($"({Positions[posNo].RowNo,0:N0}, {Positions[posNo].ColumnNo,0:N0})");
                }
                else
                {
                    sb.Append($"({Positions[posNo].RowNo + 1,0:N0}, {Positions[posNo].ColumnNo + 1,0:N0})");
                }

                if (posNo < BoardSize - 1)
                    sb.Append(", ");
            }

            if (lineNo < noOfLines - 1)
            { sb.AppendLine(); }
        }

        return sb.ToString();
    }

    private static List<Position> SetPositions(IEnumerable<int> queenPositions)
    {
        return [.. queenPositions.Select((item, index) =>
            new Position(index, item))];
    }
    #endregion PrivateMembers
}
