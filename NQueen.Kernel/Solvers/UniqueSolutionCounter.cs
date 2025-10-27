namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false)
    {
        if (boardSize <= 0) return 0;
        var uniqueKeys = new ConcurrentDictionary<UInt128, byte>();
        int[] scratch = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
        // Use a single buffer for solution reporting
        int[] solutionBuffer = new int[boardSize];
        int parallelism = Environment.ProcessorCount;
        Parallel.For(0, boardSize, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, col0 =>
        {
            var search = new BitmaskSearchEngine();
            search.Run(new BitmaskSearchEngine.Request(
                boardSize,
                RestrictFirstCol: true,
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
                    SymmetryHelper.AddIfUniquePacked(rows, uniqueKeys, scratch, out _, out _);
                    return false;
                }
            ));
        });
        if (progress == null && progressEventSource != null && sender != null)
            progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));

        return (ulong)uniqueKeys.Count;
    }
}
