namespace NQueen.Domain.Models;

public class Solution
{
    private static int _globalSequence; // fallback sequence when id not provided
    public static void ResetSequence() => Interlocked.Exchange(ref _globalSequence, 0);

    // Existing array-based constructor (kept for compatibility)
    public Solution(int[] queenPositions, ISolutionFormatter formatter, int? id = null)
    {
        if (queenPositions == null || queenPositions.Length == 0)
            throw new ArgumentException("Queen positions must be a non-empty array.", nameof(queenPositions));
        foreach (var v in queenPositions)
            if (v < 0)
                throw new ArgumentException("Queen positions must contain non-negative values.", nameof(queenPositions));
        Id = id ?? Interlocked.Increment(ref _globalSequence);
        Name = $"Solution {Id}"; // updated naming
        _queenPositions = queenPositions; // materialized upfront
        BoardSize = queenPositions.Length;
        Positions = MapQueenArrayToPositions(_queenPositions);
        _formatter = formatter;
    }

    // New packed constructor (rows packed 5 bits each into UInt128). Supports boards up to 25.
    public Solution(UInt128 packedRows, int boardSize, ISolutionFormatter formatter, int? id = null)
    {
        if (boardSize <= 0 || boardSize > 25)
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Packed storage supports board sizes 1..25.");
        Id = id ?? Interlocked.Increment(ref _globalSequence);
        Name = $"Solution {Id}"; // updated naming
        BoardSize = boardSize;
        _packed = packedRows;
        _formatter = formatter;
        Positions = new LazyPositionListLazy(this); // lazy until unpack
    }

    public int? Id { get; }
    public string Name { get; }
    public int BoardSize { get; }

    public int[] QueenPositions
    {
        get
        {
            if (_queenPositions != null) return _queenPositions;
            if (_packed.HasValue)
            {
                _queenPositions = Unpack(_packed.Value, BoardSize);
                if (Positions is LazyPositionListLazy proxy)
                    proxy.Realize(_queenPositions);
                return _queenPositions;
            }
            throw new InvalidOperationException("Solution has neither array nor packed representation.");
        }
    }

    public IReadOnlyList<Position> Positions { get; private set; }

    public string Details => _details ??= _formatter.FormatSolutions(Positions);

    public sealed override string ToString() => Name;

    private static LazyPositionList MapQueenArrayToPositions(int[] queenPositions) => new(queenPositions);

    private static int[] Unpack(UInt128 packed, int n)
    {
        var rows = new int[n];
        for (int i = n - 1; i >= 0; i--)
        {
            rows[i] = (int)(packed & 0x1F);
            packed >>= 5;
        }
        return rows;
    }

    private readonly UInt128? _packed;
    private int[]? _queenPositions;
    private readonly ISolutionFormatter _formatter;
    private string? _details;

    private sealed class LazyPositionListLazy : IReadOnlyList<Position>
    {
        private int[]? _rows;
        private readonly Solution _owner;
        public LazyPositionListLazy(Solution owner) => _owner = owner;
        public void Realize(int[] rows) => _rows = rows;
        public Position this[int index]
        {
            get
            {
                var rows = _rows;
                if (rows == null) rows = _owner.QueenPositions;
                return new Position(index, rows[index]);
            }
        }
        public int Count => _rows?.Length ?? _owner.BoardSize;
        public IEnumerator<Position> GetEnumerator()
        {
            var rows = _rows ?? _owner.QueenPositions;
            for (int i = 0; i < rows.Length; i++) yield return new Position(i, rows[i]);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
