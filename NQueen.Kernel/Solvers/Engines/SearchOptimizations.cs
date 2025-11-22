namespace NQueen.Kernel.Solvers.Engines;

/// <summary>
/// Global (thread-safe) switches for low-level search pruning optimizations that should
/// not expand the BitmaskSearchEngine.Request signature. The solver sets these before invoking Run.
/// </summary>
internal static class SearchOptimizations
{
    public static volatile bool PrefixMinimalityPruningEnabled;
    public static volatile bool ReflectionPrefixPruningEnabled;
    public static volatile bool IncrementalCanonicalizationEnabled;

    public static void Configure(bool prefixMinimality, bool reflectionPruning)
    {
        PrefixMinimalityPruningEnabled = prefixMinimality;
        ReflectionPrefixPruningEnabled = reflectionPruning;
    }

    public static void Configure(bool prefixMinimality, bool reflectionPruning, bool incrementalCanonicalization)
    {
        PrefixMinimalityPruningEnabled = prefixMinimality;
        ReflectionPrefixPruningEnabled = reflectionPruning;
        IncrementalCanonicalizationEnabled = incrementalCanonicalization;
    }
}
