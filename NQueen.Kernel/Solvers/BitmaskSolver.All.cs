namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllUnified(bool isParallel, int splitDepth)
    {
        int N = BoardSize;
        int cap = _capEnabled
            ? SimulationSettings.MaxDisplayedCount
            : int.MaxValue;

        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        object lockObj = new();
        ulong totalCount = 0;
        int materialized = 0;
        // capReachedFlag is now only for materialization, not for search termination
        int capReachedFlag = 0;

        // Only stop materializing when cap is reached, but always count all solutions
        void onSolution(int[] rows)
        {
            if (ValidateRows(rows) == false)
                return;

            lock (lockObj)
            {
                if (solutions.Count < cap)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        Volatile.Write(ref capReachedFlag, 1); // Only signals to stop materializing
                    }
                }
            }
        }

        if (isParallel && N > 1)
        {
            ulong totalCountFromEngine = 0;
            BitmaskParallelEngine.RunAllUnified(
                BoardSize,
                splitDepth,
                EnableEvents,
                cap,
                onSolution,
                count => totalCountFromEngine = count,
                pct => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken)); },
                // Only use capReachedFlag to stop materializing, not to terminate search
                () => false
            );

            totalCount = totalCountFromEngine;
        }
        else
        {
            // Sequential version
            ulong count = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled, // Never terminate early due to cap
                p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (solutions.Count < cap)
                    {
                        rawSolutions.Add(rows);
                        solutions.Add((0, rows.Length));
                        materialized++;
                        if (materialized >= cap && _capEnabled)
                        {
                            Volatile.Write(ref capReachedFlag, 1); // Only signals to stop materializing
                        }
                    }
                    count++;
                    return false; // Never terminate early due to cap
                }
            ));

            totalCount = count;
        }
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        if (EnableEvents)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllParallel(int splitDepth) => RunAllUnified(true, splitDepth);

    private void RunAllSequential() => RunAllUnified(false, ParallelRootSplitDepth);
}
