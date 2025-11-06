namespace NQueen.Domain.Utils;

public static partial class SymmetryHelper
{
    public static ulong ApplyAdvancedSymmetryPruning(int boardSize, int column, int[] queenRows, ulong availMask)
    {
        ArgumentNullException.ThrowIfNull(queenRows);
        if (boardSize <= 1) return availMask;
        if (column == 0)
        {
            int maxRow = (boardSize + 1) / 2;
            if (maxRow < boardSize) availMask &= (1UL << maxRow) - 1UL;
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
                else availMask = 0UL;
            }
        }
        return availMask;
    }

    public static bool AddIfUniquePacked(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch, out UInt128 key, out int[] canonicalCopy)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        ArgumentNullException.ThrowIfNull(scratch);
        key = 0;
        canonicalCopy = Array.Empty<int>();
        var canonSpan = GetCanonicalForm(solution, scratch, null);
        key = PackCanonical(canonSpan, canonSpan.Length);
        if (!uniqueKeys.Add(key)) return false;
        canonicalCopy = canonSpan.Length <= 32 ? canonSpan.ToArray() : Array.Empty<int>();
        return true;
    }

    public static bool AddIfUnique(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch) => AddIfUniquePacked(solution, uniqueKeys, scratch, out _, out _);

    public static int[] GetCanonicalForm(int[] solution)
    {
        ArgumentNullException.ThrowIfNull(solution);
        int n = solution.Length;
        if (n == 0) return Array.Empty<int>();
        Span<int> min = n <= 32 ? stackalloc int[n] : new int[n];
        Span<int> temp = n <= 32 ? stackalloc int[n] : new int[n];
        for (int c = 0; c < n; c++) min[c] = solution[c];
        for (int t = 1; t < 8; t++)
        {
            for (int i = 0; i < n; i++) temp[i] = -1;
            for (int c = 0; c < n; c++)
            {
                int r = solution[c];
                switch (t)
                {
                    case 1: temp[r] = n - 1 - c; break;
                    case 2: temp[n - 1 - c] = n - 1 - r; break;
                    case 3: temp[n - 1 - r] = c; break;
                    case 4: temp[n - 1 - c] = r; break;
                    case 5: temp[c] = n - 1 - r; break;
                    case 6: temp[r] = c; break;
                    case 7: temp[n - 1 - r] = n - 1 - c; break;
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
        var result = new int[n];
        for (int i = 0; i < n; i++) result[i] = min[i];
        return result;
    }

    // Canonical form with scratch; if resultBuffer provided and large enough, fill it; otherwise allocate.
    public static int[] GetCanonicalForm(int[] solution, int[] scratch, int[]? resultBuffer = null)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(scratch);
        int n = solution.Length;
        if (n == 0) return Array.Empty<int>();
        int required = n * 8;
        if (scratch.Length < required) scratch = new int[required];
        Array.Clear(scratch, 0, required);
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            scratch[0 * n + c] = r;
            scratch[1 * n + r] = n - 1 - c;
            scratch[2 * n + (n - 1 - c)] = n - 1 - r;
            scratch[3 * n + (n - 1 - r)] = c;
            scratch[4 * n + (n - 1 - c)] = r;
            scratch[5 * n + c] = n - 1 - r;
            scratch[6 * n + r] = c;
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
        }
        int[] target = resultBuffer is not null && resultBuffer.Length >= n ? resultBuffer : new int[n];
        Buffer.BlockCopy(scratch, minIdx * n * sizeof(int), target, 0, n * sizeof(int));
        return target;
    }

    public static UInt128 GetCanonicalKey(int[] solution, int[] scratch, out ReadOnlySpan<int> canonical)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(scratch);
        var canonArr = GetCanonicalForm(solution, scratch, null);
        canonical = canonArr;
        return PackCanonical(canonical, canonArr.Length);
    }

    public static int MaxRowExclusiveForColumn(int boardSize, int column, int[] queenRows) => column == 0 ? (boardSize + 1) / 2 : boardSize;
    public static int GetScratchBufferSize(int boardSize) => boardSize * 8;

    public static UInt128 PackRows(ReadOnlySpan<int> rows)
    {
        UInt128 key = 0;
        for (int i = 0; i < rows.Length; i++) key = (key << 5) | (uint)rows[i];
        return key;
    }

    public static UInt128 PackCanonical(ReadOnlySpan<int> rows, int n) =>
        PackRows(rows[..n]);

    public static IReadOnlyList<int[]> GetAllTransforms(int[] solution)
    {
        ArgumentNullException.ThrowIfNull(solution);
        int n = solution.Length;
        var result = new int[8][];
        for (int i = 0; i < 8; i++) result[i] = new int[n];
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            result[0][c] = r;
            result[1][r] = n - 1 - c;
            result[2][n - 1 - c] = n - 1 - r;
            result[3][n - 1 - r] = c;
            result[4][n - 1 - c] = r;
            result[5][c] = n - 1 - r;
            result[6][r] = c;
            result[7][n - 1 - r] = n - 1 - c;
        }
        return result;
    }

    public static bool AddIfUniquePacked(int[] solution, System.Collections.Concurrent.ConcurrentDictionary<UInt128, byte> uniqueKeys, int[] scratch, out UInt128 key, out int[] canonicalCopy)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        ArgumentNullException.ThrowIfNull(scratch);
        key = 0;
        canonicalCopy = Array.Empty<int>();
        var canonArr = GetCanonicalForm(solution, scratch, null);
        key = PackCanonical(canonArr, canonArr.Length);
        if (!uniqueKeys.TryAdd(key, 0)) return false;
        canonicalCopy = canonArr.Length <= 32 ? canonArr.ToArray() : Array.Empty<int>();
        return true;
    }

    public static bool AddIfUniquePackedReuseBuffer(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch, int[] canonicalBuffer, out UInt128 key, out int[] canonicalCopy)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        ArgumentNullException.ThrowIfNull(scratch);
        ArgumentNullException.ThrowIfNull(canonicalBuffer);
        key = 0;
        canonicalCopy = canonicalBuffer;
        var canonArr = GetCanonicalForm(solution, scratch, canonicalBuffer);
        key = PackCanonical(canonArr, canonArr.Length);
        if (!uniqueKeys.Add(key)) return false;
        canonicalCopy = canonArr;
        return true;
    }

    public static bool IsIdentityCanonical(int[] solution, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(scratch);
        int n = solution.Length;
        if (n == 0) return true;
        int required = n * 8;
        if (scratch.Length < required) scratch = new int[required];
        for (int c = 0; c < n; c++)
        {
            int r = solution[c];
            scratch[0 * n + c] = r;
            scratch[1 * n + r] = n - 1 - c;
            scratch[2 * n + (n - 1 - c)] = n - 1 - r;
            scratch[3 * n + (n - 1 - r)] = c;
            scratch[4 * n + (n - 1 - c)] = r;
            scratch[5 * n + c] = n - 1 - r;
            scratch[6 * n + r] = c;
            scratch[7 * n + (n - 1 - r)] = n - 1 - c;
        }
        for (int t = 1; t < 8; t++)
        {
            for (int i = 0; i < n; i++)
            {
                int a = scratch[t * n + i];
                int b = scratch[0 * n + i];
                if (a < b) return false;
                if (a > b) break;
            }
        }
        return true;
    }
}

