namespace NQueen.Domain.Interfaces;

public interface ISolutionFormatter
{
    string FormatSolutions(
        IReadOnlyList<Position> positions,
        IndexingType indexingType = IndexingType.OneBased,
        int noOfQueensPerLine = 40);
}