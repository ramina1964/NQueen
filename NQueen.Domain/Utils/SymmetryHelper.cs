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

    // Packed key uniqueness helper (uses UInt128). Assumes board size <= 32 (true for unique mode limit 20+ packed fits 5 bits per row up to 32).
    public static bool AddIfUniquePacked(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch, out UInt128 key, out int[] canonicalCopy)
    {
        key = 0;
        canonicalCopy = Array.Empty<int>();
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueKeys);
        if (scratch.Length < solution.Length * 2)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));
        key = GetCanonicalKey(solution, scratch, out var canonicalSpan);
        if (!uniqueKeys.Add(key))
            return false;
        // copy canonical representative so caller may materialize if desired
        canonicalCopy = canonicalSpan.ToArray();
        return true;
    }

    // Convenience overload when caller does not need key/canonical copy.
    public static bool AddIfUnique(int[] solution, HashSet<UInt128> uniqueKeys, int[] scratch) =>
        AddIfUniquePacked(solution, uniqueKeys, scratch, out _, out _);

    // Overload: GetCanonicalForm with scratch buffer (existing array-returning API)
    public static int[] GetCanonicalForm(int[] solution, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        int n = solution.Length;
        // Use first n entries for min, next n entries for buf directly (no ToArray allocations)
        var min = scratch.AsSpan(0, n);
        var buf = scratch.AsSpan(n, n);
        bool minSet = false;

        // Identity
        solution.AsSpan().CopyTo(buf);
        if (!minSet || SpanIsLess(buf, min, n)) { buf.CopyTo(min); minSet = true; }
        // Rotate 90
        for (int i = 0; i < n; i++) buf[solution[i]] = n - 1 - i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Rotate 180
        for (int i = 0; i < n; i++) buf[n - 1 - i] = n - 1 - solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Rotate 270
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect vertical
        for (int i = 0; i < n; i++) buf[n - 1 - i] = solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect horizontal
        for (int i = 0; i < n; i++) buf[i] = n - 1 - solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect main diagonal
        for (int i = 0; i < n; i++) buf[solution[i]] = i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect anti-diagonal
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = n - 1 - i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);

        // Return a cloned array for legacy API expectations
        return min.ToArray();
    }

    // Allocation-free canonical key computation.
    public static UInt128 GetCanonicalKey(int[] solution, int[] scratch, out ReadOnlySpan<int> canonical)
    {
        int n = solution.Length;
        if (scratch.Length < n * 2) throw new ArgumentException("Scratch buffer too small", nameof(scratch));
        var min = scratch.AsSpan(0, n);
        var buf = scratch.AsSpan(n, n);
        bool minSet = false;

        // Identity
        solution.AsSpan().CopyTo(buf);
        if (!minSet || SpanIsLess(buf, min, n)) { buf.CopyTo(min); minSet = true; }
        // Rotate 90
        for (int i = 0; i < n; i++) buf[solution[i]] = n - 1 - i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Rotate 180
        for (int i = 0; i < n; i++) buf[n - 1 - i] = n - 1 - solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Rotate 270
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect vertical
        for (int i = 0; i < n; i++) buf[n - 1 - i] = solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect horizontal
        for (int i = 0; i < n; i++) buf[i] = n - 1 - solution[i];
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect main diagonal
        for (int i = 0; i < n; i++) buf[solution[i]] = i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);
        // Reflect anti-diagonal
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = n - 1 - i;
        if (SpanIsLess(buf, min, n)) buf.CopyTo(min);

        canonical = min;
        return PackCanonical(min, n);
    }

    private static bool SpanIsLess(ReadOnlySpan<int> a, ReadOnlySpan<int> b, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (a[i] < b[i]) return true;
            if (a[i] > b[i]) return false;
        }
        return false;
    }

    private static UInt128 PackCanonical(ReadOnlySpan<int> rows, int n)
    {
        UInt128 key = 0;
        for (int i = 0; i < n; i++)
        {
            key = (key << 5) | (uint)rows[i];
        }
        return key;
    }

    // Legacy overload for compatibility (still used by some tests/benchmarks expecting int[] return)
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
        Array.Copy(solution, buf, n); results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[solution[i]] = n - 1 - i; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[n - 1 - i] = n - 1 - solution[i]; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = i; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[n - 1 - i] = solution[i]; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[i] = n - 1 - solution[i]; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[solution[i]] = i; results.Add((int[])buf.Clone());
        for (int i = 0; i < n; i++) buf[n - 1 - solution[i]] = n - 1 - i; results.Add((int[])buf.Clone());
        return results;
    }

    public static List<int[]> GetSymmetricalSolutions(Memory<int> solution) => GetSymmetricalSolutions(solution.ToArray());
    public static List<int[]> GetSymmetricalSolutions(ReadOnlySpan<int> solution) => GetSymmetricalSolutions(solution.ToArray());
    public static IEnumerable<int[]> GetSymmetricalTransformations(ReadOnlySpan<int> solution) => GetSymmetricalSolutions(solution.ToArray());
    public static IEnumerable<int[]> GetSymmetricalTransformations(Memory<int> solution) => GetSymmetricalSolutions(solution.ToArray());

    public static int GetScratchBufferSize(int boardSize) => boardSize * 2;
}
