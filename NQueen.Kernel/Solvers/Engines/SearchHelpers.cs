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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldPrunePrefixIncremental(
        int[] rows, int depth, int N,
        bool reflectionEnabled, bool minimalityEnabled,
        ref bool reflectionEqual, ref bool minimalityEqual)
    {
        if (reflectionEnabled && reflectionEqual)
        {
            int r = rows[depth]; if (r < 0) return false;
            int reflected = N - 1 - r;
            if (r > reflected) return true;
            if (r < reflected) reflectionEqual = false;
        }
        if (minimalityEnabled && minimalityEqual)
        {
            int first = rows[0]; if (first < 0) return false;
            int newRow = rows[depth]; if (newRow < 0) return false;
            int transformed = N - 1 - newRow;
            if (first > transformed) return true;
            if (first < transformed) minimalityEqual = false;
        }
        return false;
    }

    // Stateless forward-prefix prune for CANONICAL counting (leaf uses IsIdentityCanonical).
    // Only the horizontal-reflection test is sound here: horizontal reflection (row r -> N-1-r)
    // is column-preserving, so a prefix that is lexicographically greater than its horizontal
    // mirror can never extend to the lexicographically-minimal (canonical) representative.
    //
    // The rotate-180 "minimality" test is deliberately NOT applied: it would compare rows[i]
    // against N-1-rows[depth-i], i.e. against columns that are not yet fixed at this depth, so it
    // is unsound as a forward-prefix prune and silently under-counts (it returned 692,857 instead
    // of 1,846,955 for N=16). Full canonicality across all 8 symmetries is still enforced exactly
    // by IsIdentityCanonical at the leaf, so omitting it only costs a little extra traversal.
    //
    // Because it carries no cross-depth state, this variant stays correct even when invoked only
    // at depth >= pruneDepthGate (unlike ShouldPrunePrefixIncremental, which must run from col 0).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldPrunePrefixFull(int[] rows, int depth, int N, bool reflectionEnabled)
    {
        if (!reflectionEnabled) return false;
        for (int i = 0; i <= depth; i++)
        {
            int r = rows[i]; if (r < 0) return false;
            int reflected = N - 1 - r;
            if (r > reflected) return true;
            if (r < reflected) break;
        }
        return false;
    }
}
