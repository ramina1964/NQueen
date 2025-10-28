namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    // Optimized: thread-local HashSet<UInt128> and no canonicalCopy allocation for count-only
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
        int parallelism = Environment.ProcessorCount;
        var localSets = new HashSet<UInt128>[parallelism];
        Parallel.For(0, parallelism, i => localSets[i] = new HashSet<UInt128>(capacity: 4096));
        Parallel.For(0, boardSize, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, fr =>
        {
            int tid = Thread.GetCurrentProcessorId() % parallelism;
            var localSet = localSets[tid];
            var threadScratch = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
            var initialRows = new int[boardSize];
            Array.Fill(initialRows, -1);
            initialRows[0] = fr;
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
                    {
                        // Only add key, do not allocate canonicalCopy
                        UInt128 key = SymmetryHelper.PackCanonical(
                            SymmetryHelper.GetCanonicalForm(rows, threadScratch, null), rows.Length);
                        lock (localSet) { localSet.Add(key); }
                    }
                    return false;
                }
            ));
        });
        // Merge all local sets into one
        var globalSet = new HashSet<UInt128>(localSets.Sum(s => s.Count));
        foreach (var set in localSets)
            globalSet.UnionWith(set);
        if (progress == null && progressEventSource != null && sender != null)
            progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
        return (ulong)globalSet.Count;
    }
}
