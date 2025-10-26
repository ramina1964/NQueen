namespace NQueen.Domain.Utils;

public static partial class SymmetryHelper
{
    public static ulong ApplyAdvancedSymmetryPruning(
        int boardSize, int column, int[] queenRows, ulong availMask)
    {
        if (boardSize <= 1) return availMask;
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
                    ulong lowerMask = (1UL << minRow) - 1UL;
                    availMask &= ~lowerMask;
                }
                else
                {
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
        var canonSpan = GetCanonicalForm(solution, scratch, null); // ensure scratch populated
        key = PackCanonical(canonSpan, canonSpan.Length);
        if (!uniqueKeys.Add(key)) return false;
        canonicalCopy = canonSpan.ToArray();
        return true;
    }

    public static bool AddIfUnique(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch) => AddIfUniquePacked(solution, uniqueKeys, scratch, out _, out _);

    /// <summary>
    /// Compute canonical representative under the dihedral group of the square (8 symmetries).
    /// Returns the lexicographically minimal row-array among the8 transformed boards.
    /// </summary>
    public static ReadOnlySpan<int> GetCanonicalForm(int[] solution)
    {
        int n = solution.Length;
        if (n == 0) return ReadOnlySpan<int>.Empty;
        // Allocate8 transforms
        int[][] t = new int[8][];
        for (int i = 0; i < 8; i++) t[i] = new int[n];
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            // identity
            t[0][c] = r;
            // rotate90 CCW: (c,r)-> (r, n-1-c)
            t[1][r] = n - 1 - c;
            // rotate180: (c,r)->(n-1-c, n-1-r)
            t[2][n - 1 - c] = n - 1 - r;
            // rotate270 CCW: (c,r)->(n-1-r, c)
            t[3][n - 1 - r] = c;
            // reflect vertical: (c,r)->(n-1-c, r)
            t[4][n - 1 - c] = r;
            // reflect horizontal: (c,r)->(c, n-1-r)
            t[5][c] = n - 1 - r;
            // reflect main diagonal: (c,r)->(r,c)
            t[6][r] = c;
            // reflect anti-diagonal: (c,r)->(n-1-r, n-1-c)
            t[7][n - 1 - r] = n - 1 - c;
        }
        int minIdx = 0;
        for (int k = 1; k < 8; k++)
        {
            for (int i = 0; i < n; i++)
            {
                int a = t[k][i];
                int b = t[minIdx][i];
                if (a == b) continue;
                if (a < b) minIdx = k;
                break;
            }
        }
        return t[minIdx];
    }

    // Legacy overload with scratch & optional resultBuffer expected by tests to read all8 transforms from scratch (contiguous blocks)
    public static int[] GetCanonicalForm(int[] solution, int[] scratch, int[]? resultBuffer = null)
    {
        int n = solution.Length;
        if (n == 0) return Array.Empty<int>();
        int required = n * 8;
        if (scratch.Length < required) scratch = new int[required];
        // zero fill scratch
        for (int i = 0; i < required; i++) scratch[i] = -1;
        // block indices: b*n + i
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            // identity
            scratch[0 * n + c] = r;
            // rotate90
            scratch[1 * n + r] = n - 1 - c;
            // rotate180
            scratch[2 * n + (n - 1 - c)] = n - 1 - r;
            // rotate270
            scratch[3 * n + (n - 1 - r)] = c;
            // reflect vertical
            scratch[4 * n + (n - 1 - c)] = r;
            // reflect horizontal
            scratch[5 * n + c] = n - 1 - r;
            // reflect main diagonal
            scratch[6 * n + r] = c;
            // reflect anti-diagonal
            scratch[7 * n + (n - 1 - r)] = n - 1 - c;
        }
        int minIdx = 0;
        for (int t = 1; t < 8; t++)
        {
            for (int i = 0; i < n; i++)
            {
                int a = scratch[t * n + i];
                int b = scratch[minIdx * n + i];
                if (a == b) continue;
                if (a < b) minIdx = t;
                break;
            }
        }
        if (resultBuffer != null && resultBuffer.Length >= n)
        {
            for (int i = 0; i < n; i++) resultBuffer[i] = scratch[minIdx * n + i];
            return resultBuffer;
        }
        var res = new int[n];
        for (int i = 0; i < n; i++) res[i] = scratch[minIdx * n + i];
        return res;
    }

    public static UInt128 GetCanonicalKey(
        int[] solution, int[] scratch, out ReadOnlySpan<int> canonical)
    {
        var canonArr = GetCanonicalForm(solution, scratch, null);
        canonical = canonArr;
        return PackCanonical(canonical, canonArr.Length);
    }

    public static int MaxRowExclusiveForColumn(
        int boardSize, int column, int[] queenRows) => column == 0 ? (boardSize + 1) / 2 : boardSize;
    public static int GetScratchBufferSize(int boardSize) => boardSize * 8;

    public static UInt128 PackRows(ReadOnlySpan<int> rows)
    {
        UInt128 key = 0;
        for (int i = 0; i < rows.Length; i++) key = (key << 5) | (uint)rows[i];
        return key;
    }

    public static UInt128 PackCanonical(ReadOnlySpan<int> rows, int n) => PackRows(rows.Slice(0, n));

    public static IReadOnlyList<int[]> GetAllTransforms(int[] solution)
    {
        int n = solution.Length;
        var result = new int[8][];
        for (int i = 0; i < 8; i++) result[i] = new int[n];
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            result[0][c] = r; // identity
            result[1][r] = n - 1 - c; // rotate90
            result[2][n - 1 - c] = n - 1 - r; // rotate180
            result[3][n - 1 - r] = c; // rotate270
            result[4][n - 1 - c] = r; // reflect vertical
            result[5][c] = n - 1 - r; // reflect horizontal
            result[6][r] = c; // reflect main diagonal
            result[7][n - 1 - r] = n - 1 - c; // reflect anti-diagonal
        }
        return result;
    }
}

