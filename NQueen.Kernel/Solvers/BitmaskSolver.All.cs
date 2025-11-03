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
        ulong totalCount = 0;
        int materialized = 0;
        int capReachedFlag = 0;

        if (isParallel && N > 1)
        {
            // trackAllValues:true so .Values is valid
            var threadLocalPacked = new ThreadLocal<List<(UInt128, int)>>(() => [], true);

            // Only stop materializing when cap is reached, but always count all solutions
            void onSolution(int[] rows)
            {
                if (!ValidateRows(rows))
                    return;
                int matIdx = System.Threading.Interlocked.Increment(ref materialized);
                if (matIdx <= cap)
                {
                    // clone rows to prevent later mutation issues
                    var stored = new int[rows.Length];
                    Array.Copy(rows, stored, rows.Length);
                    var packedList = threadLocalPacked.Value;
                    if (packedList is not null)
                    {
                        packedList.Add((0, rows.Length));
                    }
                    if (_capEnabled && matIdx == cap)
                    {
                        System.Threading.Volatile.Write(ref capReachedFlag, 1);
                    }
                }
            }

            ulong totalCountFromEngine = 0;
            BitmaskParallelEngine.RunAllUnified(
                BoardSize,
                splitDepth,
                EnableEvents,
                cap,
                onSolution,
                count => totalCountFromEngine = count,
                pct => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken)); },
                // Only use capReachedFlag to stop materialization, not to terminate search
                () => false
            );

            foreach (var list in threadLocalPacked.Values) solutions.AddRange(list);
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
                        var stored = new int[rows.Length];
                        Array.Copy(rows, stored, rows.Length);
                        solutions.Add((0, rows.Length));
                        materialized++;
                        if (materialized >= cap && _capEnabled)
                        {
                            System.Threading.Volatile.Write(ref capReachedFlag, 1); // Only signals to stop materializing
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
        if (EnableEvents)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllParallel(int splitDepth) => RunAllUnified(true, splitDepth);

    private void RunAllSequential() => RunAllUnified(false, ParallelRootSplitDepth);
}
