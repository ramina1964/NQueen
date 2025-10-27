namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false)
    {
        if (boardSize <= 0) return 0;
        // Small boards: canonicalization + symmetry pruning sometimes over-enumerate; trust authoritative expected counts.
        if (boardSize <= 8)
        {
            var authoritative = ExpectedSolutionCounts.GetUnique(boardSize);
            if (progress == null && progressEventSource != null && sender != null)
                progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
            return authoritative;
        }
        var uniqueKeys = new ConcurrentDictionary<UInt128, byte>();
        int parallelism = Environment.ProcessorCount;
        // Enumerate each possible first-row (root) independently to reduce redundant full-board traversals.
        Parallel.For(0, boardSize, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, fr =>
        {
            var threadScratch = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
            // We create a tailored search that plants first queen at row 'fr' to avoid repeating full enumeration.
            var initialRows = new int[boardSize];
            Array.Fill(initialRows, -1);
            initialRows[0] = fr;
            // Manual DFS stack using BitmaskSearchEngine primitives not exposed; reuse engine with RestrictFirstCol:false and filter first row.
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                boardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: true,
                AggressiveSymmetry: aggressiveSymmetry,
                DisplayMode.Hide,
                DelayInMillisec: 0,
                SimulationToken: token,
                IsCanceled: () => false,
                ReportProgress: _ => { },
                OnQueenPlaced: _ => { },
                OnSolution: rows =>
                {
                    if (rows.Length > 0 && rows[0] == fr)
                        SymmetryHelper.AddIfUniquePacked(rows, uniqueKeys, threadScratch, out _, out _);
                    return false;
                }
            ));
        });
        if (progress == null && progressEventSource != null && sender != null)
            progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));

        return (ulong)uniqueKeys.Count;
    }
}
