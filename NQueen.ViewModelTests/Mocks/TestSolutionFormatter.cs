namespace NQueen.ViewModelTests.Mocks;

public class TestSolutionFormatter : ISolutionFormatter
{
    public string FormatSolutions(
        IReadOnlyList<Position> positions,
        IndexingType indexingType = IndexingType.OneBased,
        int noOfQueensPerLine = 40)
    {
        // Return a predictable string for testing
        return $"Formatted {positions.Count} positions";
    }
}