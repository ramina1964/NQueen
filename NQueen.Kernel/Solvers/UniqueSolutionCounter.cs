namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    // Unified: use RunUniqueUnified for both materialize and count-only
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false)
    {
        if (boardSize <= 0) return 0;
        // Always compute by enumeration, never use authoritative lookup for solver/benchmark
        ulong uniqueCount = 0;
        BitmaskSolver.RunUniqueUnifiedStatic(
            boardSize,
            parallel: true,
            cap: 0, // cap=0 disables materialization
            onMaterialized: null, // no-op
            onCounted: c => uniqueCount = c,
            reportProgress: p => { if (progress != null) progress(p); if (progressEventSource != null && sender != null) progressEventSource(sender, new ProgressUpdateEventArgs(p, token)); },
            capReached: () => false,
            aggressiveSymmetry: aggressiveSymmetry
        );
        return uniqueCount;
    }
}
