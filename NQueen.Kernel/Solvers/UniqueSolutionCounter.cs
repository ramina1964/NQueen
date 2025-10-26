namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender)
    {
        if (boardSize <= 0) return 0;
        var search = new BitmaskSearchEngine();
        var uniqueKeys = new HashSet<UInt128>();
        int[] scratch = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
        ulong solutionsObserved = 0;
        // progress: reuse root progress from engine (percentage of first-column placements examined)
        search.Run(new BitmaskSearchEngine.Request(
            boardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            DisplayMode.Hide,
            DelayInMillisec: 0,
            SimulationToken: token,
            IsCanceled: () => false,
            ReportProgress: p =>
            {
                if (progress != null) progress(p);
                else if (progressEventSource != null && sender != null)
                    progressEventSource(sender, new ProgressUpdateEventArgs(p, token));
            },
            OnQueenPlaced: _ => { },
            OnSolution: rows =>
            {
                // rows is a full solution; canonicalize and add
                var copy = (int[])rows.Clone();
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratch, out _, out _))
                {
                    solutionsObserved++;
                }
                return false; // continue enumeration
            }
        ));
        if (progress == null && progressEventSource != null && sender != null)
            progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
        return (ulong)uniqueKeys.Count; // fundamental unique solutions
    }
}
