namespace NQueen.Kernel.Solvers.Engines;

/// <summary>
/// Global (thread-safe) switches for low-level search pruning optimizations that should
/// not expand the BitmaskSearchEngine.Request signature. The solver sets these before invoking Run.
/// </summary>
internal static class SearchOptimizations
{

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
}
