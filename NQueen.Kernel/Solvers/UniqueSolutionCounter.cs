namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    // Unified: use symmetry-pruned for large boards, canonical for small
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false, int cap = 0, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        ulong uniqueCount = 0;
        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            // Use symmetry-pruned unique counter for large boards
            uniqueCount = SymmetryPrunedUniqueCounter.Count(boardSize, cap, onMaterialized);
        }
        else
        {
            // Use canonicalization-based for small boards
            BitmaskSolver.RunUniqueUnifiedStatic(
                boardSize,
                parallel: true,
                cap: cap,
                onMaterialized: onMaterialized,
                onCounted: c => uniqueCount = c,
                reportProgress: p => { if (progress != null) progress(p); if (progressEventSource != null && sender != null) progressEventSource(sender, new ProgressUpdateEventArgs(p, token)); },
                capReached: () => false,
                aggressiveSymmetry: aggressiveSymmetry
            );
        }
        return uniqueCount;
    }
}
