namespace NQueen.Kernel.Solvers;

/// <summary>
/// Heuristic for selecting optimal parallel root split depth based on board size and core count.
/// </summary>
internal static class ParallelSplitDepthHeuristic
{
    public static int GetOptimalSplitDepth(int boardSize, int? logicalCoreCount = null)
    {
        int cores = logicalCoreCount ?? Environment.ProcessorCount;
        if (boardSize <= 10)
            return 1;
        if (boardSize <= 13)
            return 2;
        if (boardSize >= 14)
        {
            // Estimate fan-out: N!/(N-splitDepth)!; clamp to avoid excessive tasks
            int maxDepth = 3;
            int estimatedTasks = (int)Math.Pow(boardSize, maxDepth);
            int maxTasks = cores * 4;
            if (estimatedTasks > maxTasks)
                return 2;
            return maxDepth;
        }
        return 1;
    }
}
