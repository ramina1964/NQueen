namespace NQueen.Domain.Models;

public class Solution
{
    public Solution(int[] queenPositions, ISolutionFormatter formatter, int? id = null)
    {
        if (queenPositions == null || queenPositions.Length == 0)
            throw new ArgumentException("Queen positions must be a non-empty array.", nameof(queenPositions));

        if (queenPositions.Any(pos => pos < 0))
            throw new ArgumentException("Queen positions must contain non-negative values.", nameof(queenPositions));

        BoardSize = queenPositions.Length;
        Id = id;
        Name = $"No. {id}";
        QueenPositions = queenPositions;
        Positions = MapQueenArrayToPositions(QueenPositions);
        Details = formatter.FormatSolutions(Positions);
    }

    public IReadOnlyList<Position> Positions { get; }

    public int? Id { get; }

    public string Name { get; }

    public int[] QueenPositions { get; }

    public string Details { get; }

    public sealed override string ToString() => Name;

    private int BoardSize { get; }

    private static LazyPositionList MapQueenArrayToPositions(int[] queenPositions) =>
        new(queenPositions);
}
