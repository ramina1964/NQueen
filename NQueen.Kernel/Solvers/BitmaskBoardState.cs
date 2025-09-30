namespace NQueen.Kernel.Solvers;

/// <summary>
/// Bitmask-specific board state for N-Queens search (initial extraction).
/// Renamed from BoardState to BitmaskBoardState to avoid ambiguity with existing core BoardState.
/// </summary>
public sealed class BitmaskBoardState
{
    private BitmaskBoardState(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (size > 31)
            throw new NotSupportedException("BitmaskBoardState currently supports N <= 31 for bitmask operations.");

        Size = size;
        Rows = new int[size];
        Array.Fill(Rows, -1);
        FullMask = size == 32 ? 0xFFFFFFFFu : (uint)((1u << size) - 1);
    }

    public static BitmaskBoardState Create(int size) => new(size);

    public int Size { get; }

    public int[] Rows { get; }

    public int Col { get; private set; }

    public uint ColMask { get; private set; }

    public uint Diag1Mask { get; private set; }

    public uint Diag2Mask { get; private set; }

    public uint FullMask { get; }

    public bool IsComplete => Col == Size;

    // Available = NOT (occupied columns OR main diagonals OR anti diagonals) & fullMask
    public uint GetAvailableMask() =>
        ~(ColMask | Diag1Mask | Diag2Mask) & FullMask;

    public void Place(int row)
    {
        if (row < 0 || row >= Size) throw new ArgumentOutOfRangeException(nameof(row));
        if (Col >= Size) throw new InvalidOperationException("Board already complete.");
        uint bit = 1u << row;
        Rows[Col] = row;
        ColMask |= bit;
        Diag1Mask = (Diag1Mask | bit) << 1;
        Diag2Mask = (Diag2Mask | bit) >> 1;
        Col++;
    }

    public void Backtrack(uint previousColMask, uint previousDiag1, uint previousDiag2)
    {
        if (Col == 0) throw new InvalidOperationException("Cannot backtrack from empty state.");
        Col--;
        Rows[Col] = -1;
        ColMask = previousColMask;
        Diag1Mask = previousDiag1;
        Diag2Mask = previousDiag2;
    }

    public (uint colMask, uint diag1Mask, uint diag2Mask) SnapshotMasks() =>
        (ColMask, Diag1Mask, Diag2Mask);

    public int[] CloneRows()
    {
        var copy = new int[Size];
        Array.Copy(Rows, copy, Size);
        return copy;
    }
}
