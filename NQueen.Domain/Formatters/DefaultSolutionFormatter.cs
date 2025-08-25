namespace NQueen.Domain.Formatters;

public class DefaultSolutionFormatter : ISolutionFormatter
{
    public string FormatSolutions(
        IReadOnlyList<Position> positions,
        IndexingType indexingType = IndexingType.OneBased,
        int noOfQueensPerLine = 40)
    {
        return string.Join(", ", positions.Select(p => $"({p.ColumnIndex}, {p.RowIndex})"));
    }
}