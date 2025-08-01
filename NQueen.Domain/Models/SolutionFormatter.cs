namespace NQueen.Domain.Models;

public static class SolutionFormatter
{
    public static string FormatSolutions(
        List<Position> positions,
        IndexingType indexingType = IndexingType.OneBased,
        int noOfQueensPerLine = 40)
    {
        var columnOrdered = positions
            .OrderBy(p => p.ColumnIndex)
            .ToList();

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
        List<Position> positions, int lineLength)
    {
        var lines = new List<List<Position>>();
        for (int i = 0; i < positions.Count; i += lineLength)
            lines.Add([.. positions.Skip(i).Take(lineLength)]);

        return lines;
    }

    private static string FormatPosition(Position p, IndexingType indexingType) =>
        indexingType == IndexingType.ZeroBased
            ? $"({p.ColumnIndex},{p.RowIndex})"
            : $"({p.ColumnIndex + 1},{p.RowIndex + 1})";
}
