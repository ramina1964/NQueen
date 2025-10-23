namespace NQueen.Domain.Utils;

public static partial class SymmetryHelper
{
    public static ulong ApplyAdvancedSymmetryPruning(
        int boardSize, int column, int[] queenRows, ulong availMask)
    {
        if (boardSize <= 1)
            return availMask;

        if (column == 0)
        {
            int maxRow = (boardSize + 1) / 2;
            if (maxRow < boardSize)
                availMask &= (1UL << maxRow) - 1UL;
            return availMask;
        }
        if (column == 1)
        {
            int firstRow = queenRows[0];
            if (firstRow >= 0 && !((boardSize & 1) == 1 && firstRow == boardSize / 2))
            {
                int minRow = firstRow + 1;
                if (minRow < boardSize)
                {
                    ulong lowerMask = (1UL << minRow) - 1UL; // rows < minRow
                    availMask &= ~lowerMask;
                }
                else
                {
                    // No valid placements remain; return 0 mask.
                    availMask = 0UL;
                }
            }
        }
        return availMask;
    }

    public static bool AddIfUniquePacked(
        int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch,
        out UInt128 key, out int[] canonicalCopy)
    {
        key = 0;
        canonicalCopy = Array.Empty<int>();
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);

        // Only require minimum scratch of2n (legacy callers); canonicalizer will internally allocate if larger needed.
        if (scratch.Length < solution.Length * 2)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));

        key = GetCanonicalKey(solution, scratch, out var canonicalSpan);
        if (!uniqueKeys.Add(key))
            return false;

        canonicalCopy = canonicalSpan.ToArray();
        return true;
    }

    // Convenience overload when caller does not need key/canonical copy.
    public static bool AddIfUnique(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch) =>
        AddIfUniquePacked(solution, uniqueKeys, scratch, out _, out _);

    // Canonical form generating all8 dihedral transforms. Falls back to internal buffer if caller scratch too small.
    public static int[] GetCanonicalForm(int[] solution, int[] scratch, int[]? resultBuffer = null)
    {
        int n = solution.Length;
        if (n ==0) return Array.Empty<int>();

        int required = n *8;
        Span<int> work = scratch.Length >= required ? scratch : stackalloc int[required];

        // Layout: t0..t7 each length n in contiguous blocks
        solution.CopyTo(work.Slice(0, n)); // t0 identity
        for (int b =1; b <8; b++)
            for (int i =0; i < n; i++) work[b * n + i] = -1;

        for (int c =0; c < n; c++)
        {
            int r = solution[c];
            work[n + (n -1 - r)] = c; // t1 rotate90
            work[2 * n + (n -1 - c)] = n -1 - r; // t2 rotate180
            work[3 * n + r] = n -1 - c; // t3 rotate270
            work[4 * n + (n -1 - c)] = r; // t4 reflect vertical
            work[5 * n + c] = n -1 - r; // t5 reflect horizontal
            work[6 * n + r] = c; // t6 reflect main diagonal
            work[7 * n + (n -1 - r)] = n -1 - c; // t7 reflect anti-diagonal
        }

        int minIdx =0;
        for (int t =1; t <8; t++)
        {
            int cmp =0;
            for (int i =0; i < n; i++)
            {
                int a = work[t * n + i];
                int b = work[minIdx * n + i];
                if (a == b) continue;
                cmp = a - b;
                break;
            }
            if (cmp <0) minIdx = t;
        }

        if (resultBuffer != null && resultBuffer.Length >= n)
        {
            for (int i =0; i < n; i++) resultBuffer[i] = work[minIdx * n + i];
            return resultBuffer;
        }
        else
        {
            int[] result = new int[n];
            for (int i =0; i < n; i++) result[i] = work[minIdx * n + i];
            return result;
        }
    }

    public static UInt128 GetCanonicalKey(
        int[] solution, int[] scratch, out ReadOnlySpan<int> canonical)
    {
        int[] canon = GetCanonicalForm(solution, scratch);
        canonical = canon;
        
        return PackCanonical(canonical, canonical.Length);
    }

    public static int MaxRowExclusiveForColumn(
        int boardSize, int column, int[] queenRows) =>
            column == 0 ? (boardSize + 1) / 2 : boardSize;

    public static int GetScratchBufferSize(int boardSize) => boardSize * 8;

    public static UInt128 PackCanonical(ReadOnlySpan<int> rows, int n)
    {
        UInt128 key = 0;
        for (int i = 0; i < n; i++)
            key = (key << 5) | (uint)rows[i];
        
        return key;
    }
}

