namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Domain.Settings;

internal static class UniqueSolutionCounter
{
    // Unified: use symmetry-pruned for large boards, canonical for small
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false, int cap = 0, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            // Use symmetry-pruned unique counter for large boards
            return SymmetryPrunedUniqueCounter.Count(boardSize, cap, onMaterialized);
        }
        else
        {
            // Use canonicalization-based for small boards
            ulong uniqueCount = 0;
            CanonicalUniqueSearchEngine.CountUnique(boardSize, onMaterialized);
            // If onMaterialized is not null, it will be called for each solution; otherwise, just count
            uniqueCount = CanonicalUniqueSearchEngine.CountUnique(boardSize, null);
            return uniqueCount;
        }
    }
}
