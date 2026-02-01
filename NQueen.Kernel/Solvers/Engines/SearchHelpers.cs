namespace NQueen.Kernel.Solvers.Engines;

internal static class SearchHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOddCenterFirstRow(int n, int r) => ((n & 1) == 1) && r == (n / 2);

    // Returns (key, canonicalRows[]) using identity fast-path when already minimal.
    public static (UInt128 key, int[] canonicalRows) PackIdentityIfCanonical(int[] rows, int[] scratch, int n)
    {
        if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
        {
            // Identity is canonical: pack and return a copy (materialization-safe)
            var copy = new int[n];
            Array.Copy(rows, copy, n);
            return (SymmetryHelper.PackRows(rows), copy);
        }
        // Compute canonical form and return a materialized array
        var key = SymmetryHelper.GetCanonicalKey(rows, scratch, out var span);
        var canon = new int[n];
        span.CopyTo(canon);
        return (key, canon);
    }
}
