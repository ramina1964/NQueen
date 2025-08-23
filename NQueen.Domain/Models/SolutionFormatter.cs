namespace NQueen.Domain.Models;

public static class SolutionFormatter
{
    public static string FormatSolutions(
        List<Position> positions,
        IndexingType indexingType = IndexingType.OneBased,
        int noOfQueensPerLine = 40)
    {
        var columnOrdered = positions
            .OrderBy(p => p.ColumnIndex);

        var lines = SplitIntoLines(columnOrdered, noOfQueensPerLine);

        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var formattedPositions = lines[i].Select(p => FormatPosition(p, indexingType));
            sb.Append(string.Join(", ", formattedPositions));
            if (i < lines.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    private static List<List<Position>> SplitIntoLines(
        IEnumerable<Position> positions, int lineLength)
    {
        var lines = new List<List<Position>>();
        var currentLine = new List<Position>();

        foreach (var position in positions)
        {
            currentLine.Add(position);
            if (currentLine.Count == lineLength)
            {
                lines.Add(currentLine);
                currentLine = new List<Position>();
            }
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    private static string FormatPosition(Position p, IndexingType indexingType) =>
        indexingType == IndexingType.ZeroBased
            ? $"({p.ColumnIndex},{p.RowIndex})"
            : $"({p.ColumnIndex + 1},{p.RowIndex + 1})";
}
