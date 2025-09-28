namespace NQueen.KernelBitmask.Solvers;

/// <summary>
/// Bitmask-specific board state for N-Queens search (initial extraction).
/// Renamed from BoardState to BitmaskBoardState to avoid ambiguity with existing core BoardState.
/// </summary>
public sealed class BitmaskBoardState
{
    private BitmaskBoardState(int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (size > 31) throw new NotSupportedException("BitmaskBoardState currently supports N <= 31 for bitmask operations.");
        Size = size;
        Rows = new int[size];
        Array.Fill(Rows, -1);
        FullMask = size == 32 ? 0xFFFFFFFFu : (uint)((1u << size) - 1);
    }

    /// <summary>Create a fresh empty board state.</summary>
    public static BitmaskBoardState Create(int size) => new(size);

    /// <summary>Number of queens / columns on the board.</summary>
    public int Size { get; }

    /// <summary>Row positions per column (unassigned = -1).</summary>
    public int[] Rows { get; }

    /// <summary>Current search depth == next column to place.</summary>
    public int Col { get; private set; }

    /// <summary>Bitmask of used rows (columns from classic viewpoint).</summary>
    public uint ColMask { get; private set; }

    /// <summary>Main diagonal mask (shifted left each level in classic bitmask algorithm).</summary>
    public uint Diag1Mask { get; private set; }

    /// <summary>Anti-diagonal mask (shifted right each level).</summary>
    public uint Diag2Mask { get; private set; }

    /// <summary>Mask with low N bits set.</summary>
    public uint FullMask { get; }

    /// <summary>True when all queens placed.</summary>
    public bool IsComplete => Col == Size;

    /// <summary>Compute available row bitmask for the current column.</summary>
    public uint GetAvailableMask()
    {
        // Available = NOT (occupied columns OR main diagonals OR anti diagonals) & fullMask
        return ~(ColMask | Diag1Mask | Diag2Mask) & FullMask;
    }

    /// <summary>Place a queen in the next column at a given row.</summary>
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

    /// <summary>Undo the last placement (simple stack-pop). Assumes at least one queen placed.</summary>
    public void Backtrack(uint previousColMask, uint previousDiag1, uint previousDiag2)
    {
        if (Col == 0) throw new InvalidOperationException("Cannot backtrack from empty state.");
        Col--;
        Rows[Col] = -1;
        ColMask = previousColMask;
        Diag1Mask = previousDiag1;
        Diag2Mask = previousDiag2;
    }

    /// <summary>Capture current masks (used before Place to allow Backtrack).</summary>
    public (uint colMask, uint diag1Mask, uint diag2Mask) SnapshotMasks() => (ColMask, Diag1Mask, Diag2Mask);

    /// <summary>Clone only the rows (for solution storage) without duplicating masks.</summary>
    public int[] CloneRows()
    {
        var copy = new int[Size];
        Array.Copy(Rows, copy, Size);
        return copy;
    }
}
