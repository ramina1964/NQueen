namespace NQueen.Kernel.Solvers.Engines;

internal static class SearchHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOddCenterFirstRow(int n, int r) => ((n & 1) == 1) && r == (n / 2);

    // Returns canonical key (always), and canonical rows only when materialization is requested.
    // Uses identity fast-path when already minimal.
    public static UInt128 PackIdentityKey(int[] rows, int[] scratch)
    {
        if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
            return SymmetryHelper.PackRows(rows);

        // We don't need canonical rows; still compute canonical key efficiently
        return SymmetryHelper.GetCanonicalKey(rows, scratch, out _);
    }

    // Returns both canonical key and canonical rows (allocates a new array for rows).
    public static (UInt128 key, int[] canonicalRows) PackIdentityKeyAndRows(int[] rows, int[] scratch, int n)
    {
        if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
        {
            var copy = new int[n];
            Array.Copy(rows, copy, n);
            return (SymmetryHelper.PackRows(rows), copy);
        }

        var key = SymmetryHelper.GetCanonicalKey(rows, scratch, out var span);
        var canon = new int[n];
        span.CopyTo(canon);
        return (key, canon);
    }
}
