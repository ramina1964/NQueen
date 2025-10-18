namespace NQueen.Domain.Models;

public class SolutionFormatter : ISolutionFormatter
{
    public string FormatSolutions(
        IReadOnlyList<Position> positions,
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

    public static string UpdateSolutionLabel(SolutionMode solutionMode) =>
        solutionMode == SolutionMode.Single
                ? $"Solution"
                : solutionMode == SolutionMode.Unique
                ? $"Unique Solutions (Max Displayed: {SimulationSettings.MaxNoOfSolutionsInOutput})"
                : $"All Solutions (Max Displayed: {SimulationSettings.MaxNoOfSolutionsInOutput})";

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
                currentLine = [];
            }
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    private static string FormatPosition(Position p, IndexingType indexingType)
    {
        return indexingType == IndexingType.ZeroBased
            ? $"({p.ColumnIndex},{p.RowIndex})"
            : $"({p.ColumnIndex + 1},{p.RowIndex + 1})";
    }
}
