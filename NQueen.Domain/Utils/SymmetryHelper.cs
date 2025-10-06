namespace NQueen.Domain.Utils;

public static partial class SymmetryHelper
{
    public static int MaxRowExclusiveForColumn(int boardSize, int column, int[] queenRows)
    {
        if (column == 0)
            return (boardSize + 1) / 2;
        if (column == 1)
        {
            int firstRow = queenRows[0];
            if ((boardSize & 1) == 1 && firstRow == boardSize / 2)
                return boardSize / 2; // strictly above center
        }
        return boardSize;
    }

    // Optimized: pass scratch buffer to avoid allocations
    public static bool AddIfUnique(int[] solution, HashSet<int[]> uniqueSolutions, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueSolutions);
        ArgumentNullException.ThrowIfNull(scratch);
        if (scratch.Length < solution.Length * 2)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));

        // Compute canonical form using scratch
        var canonical = GetCanonicalForm(solution, scratch);
        if (uniqueSolutions.Contains(canonical))
            return false;
        uniqueSolutions.Add((int[])canonical.Clone());
        return true;
    }

    // Overload: GetCanonicalForm with scratch buffer
    public static int[] GetCanonicalForm(int[] solution, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        int n = solution.Length;
        // Use provided scratch buffer for all transformations
        int[] min = scratch.AsSpan(0, n).ToArray();
        int[] buf = scratch.AsSpan(n, n).ToArray();
        bool minSet = false;

        // Identity
        Array.Copy(solution, buf, n);
        if (!minSet || IsLess(buf, min, n)) { Array.Copy(buf, min, n); minSet = true; }

        // Rotate 90
        for (int i = 0; i < n; i++) buf[solution[i]] = n - 1 - i;
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Rotate 180
        for (int i = 0; i < n; i++) buf[n - 1 - i] = n - 1 - solution[i];
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Rotate 270
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = i;
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Reflect vertical
        for (int i = 0; i < n; i++) buf[n - 1 - i] = solution[i];
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Reflect horizontal
        for (int i = 0; i < n; i++) buf[i] = n - 1 - solution[i];
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Reflect main diagonal
        for (int i = 0; i < n; i++) buf[solution[i]] = i;
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        // Reflect anti-diagonal
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = n - 1 - i;
        if (IsLess(buf, min, n)) Array.Copy(buf, min, n);

        return min;
    }

    // Legacy overload for compatibility
    public static int[] GetCanonicalForm(int[] solution)
    {
        int n = solution.Length;
        int[] scratch = new int[n * 2];
        return GetCanonicalForm(solution, scratch);
    }

    private static bool IsLess(int[] a, int[] b, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (a[i] < b[i]) return true;
            if (a[i] > b[i]) return false;
        }
        return false;
    }

    // Restore public API for compatibility
    public static List<int[]> GetSymmetricalSolutions(int[] solution)
    {
        int n = solution.Length;
        var results = new List<int[]>(8);
        var buf = new int[n];
        // 1. Identity
        Array.Copy(solution, buf, n);
        results.Add((int[])buf.Clone());
        // 2. Rotate 90
        for (int i = 0; i < n; i++) buf[solution[i]] = n - 1 - i;
        results.Add((int[])buf.Clone());
        // 3. Rotate 180
        for (int i = 0; i < n; i++) buf[n - 1 - i] = n - 1 - solution[i];
        results.Add((int[])buf.Clone());
        // 4. Rotate 270
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = i;
        results.Add((int[])buf.Clone());
        // 5. Reflect vertical
        for (int i = 0; i < n; i++) buf[n - 1 - i] = solution[i];
        results.Add((int[])buf.Clone());
        // 6. Reflect horizontal
        for (int i = 0; i < n; i++) buf[i] = n - 1 - solution[i];
        results.Add((int[])buf.Clone());
        // 7. Reflect main diagonal
        for (int i = 0; i < n; i++) buf[solution[i]] = i;
        results.Add((int[])buf.Clone());
        // 8. Reflect anti-diagonal
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = n - 1 - i;
        results.Add((int[])buf.Clone());
        return results;
    }

    public static List<int[]> GetSymmetricalSolutions(Memory<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    public static List<int[]> GetSymmetricalSolutions(ReadOnlySpan<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    public static IEnumerable<int[]> GetSymmetricalTransformations(ReadOnlySpan<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    public static IEnumerable<int[]> GetSymmetricalTransformations(Memory<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    public static int GetScratchBufferSize(int boardSize) =>
        boardSize * 2;
}
