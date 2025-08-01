namespace NQueen.Domain.Models;

public static class SolutionFormatter
{
    public static string FormatDetails(
        List<Position> positions,
        int boardSize,
        IndexingType indexingType = IndexingType.ZeroBased,
        int noOfQueensPerLine = 40)
    {
        // Order by column index ascending
        var columnOrdered = positions
            .OrderBy(p => p.ColumnIndex)
            .ToList();

        var noOfLines = boardSize % noOfQueensPerLine == 0 ?
            boardSize / noOfQueensPerLine :
            boardSize / noOfQueensPerLine + 1;

        StringBuilder sb = new();
        for (var lineNo = 0; lineNo < noOfLines; lineNo++)
        {
            var maxQueensInLastLine = lineNo < noOfLines - 1 || boardSize % noOfQueensPerLine == 0 ?
                noOfQueensPerLine :
                Math.Min(boardSize % noOfQueensPerLine, noOfQueensPerLine);

            for (var posInLine = 0; posInLine < maxQueensInLastLine; posInLine++)
            {
                var posNo = noOfQueensPerLine * lineNo + posInLine;
                if (posNo >= columnOrdered.Count)
                    break;

                if (indexingType == IndexingType.ZeroBased)
                    sb.Append($"({columnOrdered[posNo].ColumnIndex,0:N0},{columnOrdered[posNo].RowIndex,0:N0})");
                else
                    sb.Append($"({columnOrdered[posNo].ColumnIndex + 1,0:N0},{columnOrdered[posNo].RowIndex + 1,0:N0})");

                if (posNo < boardSize - 1)
                    sb.Append(", ");
            }

            if (lineNo < noOfLines - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }
}