namespace NQueen.Domain.Models;

public class Solution
{
    public Solution(int[] queenPositions, int? id = null)
    {
        BoardSize = queenPositions.Length;
        Id = id;
        Name = ToString();
        QueenPositions = queenPositions;
        Positions = MapQueenArrayToPositions(QueenPositions);
        Details = SolutionFormatter.FormatSolutions(Positions, IndexingType.ZeroBased);
    }

    public List<Position> Positions { get; set; }

    public int? Id { get; }

    public string Name { get; set; }

    public int[] QueenPositions { get; }

    public string Details { get; set; }

    public sealed override string ToString() => $"No. {Id}";

    private int BoardSize { get; }

    private static List<Position> MapQueenArrayToPositions(int[] queenPositions) =>
        [.. queenPositions.Select((rowIndex, columnIndex) =>
            new Position(columnIndex, rowIndex))];
}
