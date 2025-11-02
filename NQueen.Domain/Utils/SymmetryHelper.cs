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
        // Avoid ToArray: use a stackalloc buffer for small N, or reuse scratch for canonical
        var canonSpan = GetCanonicalForm(solution, scratch, null);
        key = PackCanonical(canonSpan, canonSpan.Length);
        if (!uniqueKeys.Add(key)) return false;
        // Only materialize canonicalCopy if needed (for tests/UI)
        canonicalCopy = canonSpan.Length <= 32 ? canonSpan.ToArray() : Array.Empty<int>();
        return true;
    }

    public static bool AddIfUnique(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch) => AddIfUniquePacked(solution, uniqueKeys, scratch, out _, out _);

    /// <summary>
    /// Compute canonical representative under the dihedral group of the square (8 symmetries).
    /// Returns the lexicographically minimal row-array among the8 transformed boards.
    /// </summary>
    public static int[] GetCanonicalForm(int[] solution)
    {
        int n = solution.Length;
        if (n == 0) return Array.Empty<int>();
        Span<int> min = n <= 32 ? stackalloc int[n] : new int[n];
        Span<int> temp = n <= 32 ? stackalloc int[n] : new int[n];
        // identity
        for (int c = 0; c < n; c++) min[c] = solution[c];
        // Try all 7 other transforms
        for (int t = 1; t < 8; t++)
        {
            for (int i = 0; i < n; i++) temp[i] = -1;
            for (int c = 0; c < n; c++)
            {
                int r = solution[c];
                switch (t)
                {
                    case 1: temp[r] = n - 1 - c; break; // rotate90
                    case 2: temp[n - 1 - c] = n - 1 - r; break; // rotate180
                    case 3: temp[n - 1 - r] = c; break; // rotate270
                    case 4: temp[n - 1 - c] = r; break; // reflect vertical
                    case 5: temp[c] = n - 1 - r; break; // reflect horizontal
                    case 6: temp[r] = c; break; // reflect main diagonal
                    case 7: temp[n - 1 - r] = n - 1 - c; break; // reflect anti-diagonal
                }
            }
            bool isLess = false;
            for (int i = 0; i < n; i++)
            {
                if (temp[i] < min[i]) { isLess = true; break; }
                if (temp[i] > min[i]) break;
            }
            if (isLess)
                for (int i = 0; i < n; i++) min[i] = temp[i];
        }
        // Copy min to array for safe return
        var result = new int[n];
        for (int i = 0; i < n; i++) result[i] = min[i];
        return result;
    }

    // Legacy overload with scratch & optional resultBuffer expected by tests to read all8 transforms from scratch (contiguous blocks)
    // OPTIMIZED: Early exit and minimal copying if identity is already minimal
    public static int[] GetCanonicalForm(int[] solution, int[] scratch, int[]? resultBuffer = null)
    {
        int n = solution.Length;
        if (n == 0) return Array.Empty<int>();
        int required = n * 8;
        if (scratch.Length < required) scratch = new int[required];
        // zero fill scratch
        Array.Clear(scratch, 0, required);
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
        bool identityIsMin = true;
        for (int t = 1; t < 8; t++)
        {
            bool isLess = false;
            for (int i = 0; i < n; i++)
            {
                int a = scratch[t * n + i];
                int b = scratch[minIdx * n + i];
                if (a < b) { isLess = true; break; }
                if (a > b) break;
            }
            if (isLess)
            {
                minIdx = t;
                identityIsMin = false;
            }
            // Early exit: if identity is already minimal and no transform is less, skip copying
            if (t == 7 && identityIsMin && minIdx == 0)
            {
                if (resultBuffer != null && resultBuffer.Length >= n)
                {
                    Buffer.BlockCopy(solution, 0, resultBuffer, 0, n * sizeof(int));
                    return resultBuffer;
                }
                var res = new int[n];
                Buffer.BlockCopy(solution, 0, res, 0, n * sizeof(int));
                return res;
            }
        }
        if (resultBuffer != null && resultBuffer.Length >= n)
        {
            Buffer.BlockCopy(scratch, minIdx * n * sizeof(int), resultBuffer,0, n * sizeof(int));
            return resultBuffer;
        }
        var result = new int[n];
        Buffer.BlockCopy(scratch, minIdx * n * sizeof(int), result, 0, n * sizeof(int));
        return result;
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

    public static bool AddIfUniquePacked(
        int[] solution, System.Collections.Concurrent.ConcurrentDictionary<UInt128, byte> uniqueKeys, int[] scratch,
        out UInt128 key, out int[] canonicalCopy)
    {
        key = 0;
        canonicalCopy = Array.Empty<int>();
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        var canonArr = GetCanonicalForm(solution, scratch, null);
        key = PackCanonical(canonArr, canonArr.Length);
        if (!uniqueKeys.TryAdd(key, 0)) return false;
        canonicalCopy = canonArr.Length <= 32 ? canonArr.ToArray() : Array.Empty<int>();
        return true;
    }

    public static bool AddIfUniquePackedReuseBuffer(
        int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch, int[] canonicalBuffer,
        out UInt128 key, out int[] canonicalCopy)
    {
        key = 0;
        canonicalCopy = canonicalBuffer;
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        // Use allocation-free canonicalization into canonicalBuffer
        var canonArr = GetCanonicalForm(solution, scratch, canonicalBuffer);
        key = PackCanonical(canonArr, canonArr.Length);
        if (!uniqueKeys.Add(key)) return false;
        canonicalCopy = canonArr;
        return true;
    }
}

