namespace NQueen.Domain.Models;

// Todo: Use this class instead of creating Position instances in large loops.
public class LazyPositionList(int[] queenPositions) : IReadOnlyList<Position>
{
    private readonly int[] _queenPositions = queenPositions ??
            throw new ArgumentNullException(nameof(queenPositions));

    public Position this[int index] => new(index, _queenPositions[index]);

    public int Count => _queenPositions.Length;

    public IEnumerator<Position> GetEnumerator()
    {
        for (int i = 0; i < _queenPositions.Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}