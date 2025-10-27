namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllUnified(bool parallel, int splitDepth)
    {
        int N = BoardSize;
        int cap = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        object lockObj = new();
        ulong totalCount =0;
        int materialized =0;
        int capReachedFlag =0; //0 = not reached,1 = reached
        int lastPct = -1;
        ulong expectedTotal = ExpectedSolutionCounts.GetAll(N);

        Action<int[]> onSolution = rows =>
        {
            if (Volatile.Read(ref capReachedFlag) ==1) return;
            if (!ValidateRows(rows)) return;
            lock (lockObj)
            {
                if (Volatile.Read(ref capReachedFlag) ==1) return;
                if (solutions.Count < cap)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        Volatile.Write(ref capReachedFlag,1);
                    }
                }
            }
            if (EnableEvents && expectedTotal >0)
            {
                int pctApprox = (int)Math.Min(100.0, (double)solutions.Count / expectedTotal *100.0);
                if (pctApprox != lastPct)
                {
                    lastPct = pctApprox;
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pctApprox, _currentSimToken));
                }
            }
        };

        if (parallel && N >1)
        {
            ulong totalCountFromEngine =0;
            _parallelEngine.RunAllUnified(
                BoardSize,
                splitDepth,
                EnableEvents,
                cap,
                onSolution,
                count => totalCountFromEngine = count,
                pct =>
                {
                    if (EnableEvents && expectedTotal ==0)
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                },
                () => Volatile.Read(ref capReachedFlag) ==1
            );
            // After cap, run count-only for the rest if needed
            if (materialized >= cap)
            {
                ulong countOnly =0;
                _parallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                    BoardSize,
                    splitDepth,
                    c => countOnly = c,
                    pct => { }
                ));
                totalCountFromEngine = countOnly;
            }
            totalCount = totalCountFromEngine;
        }
        else
        {
            // Sequential version
            ulong count =0;
            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled || Volatile.Read(ref capReachedFlag) ==1,
                p => { if (EnableEvents && expectedTotal ==0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                rows =>
                {
                    if (Volatile.Read(ref capReachedFlag) ==1) return true;
                    if (!ValidateRows(rows)) return false;
                    if (solutions.Count < cap)
                    {
                        rawSolutions.Add(rows);
                        solutions.Add((0, rows.Length));
                        materialized++;
                        if (materialized >= cap && _capEnabled)
                        {
                            Volatile.Write(ref capReachedFlag,1);
                            return true;
                        }
                    }
                    count++;
                    if (EnableEvents && expectedTotal >0)
                    {
                        int pct = (int)Math.Min(100.0, (double)count / expectedTotal *100.0);
                        if (pct != lastPct)
                        {
                            lastPct = pct;
                            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                        }
                    }
                    return false;
                }
            ));
            // After cap, run count-only for the rest if needed
            if (materialized >= cap)
            {
                ulong countOnly =0;
                _searchEngine.Run(new BitmaskSearchEngine.Request(
                    BoardSize,
                    RestrictFirstCol: false,
                    EnhancedSymmetry: false,
                    AggressiveSymmetry: false,
                    DisplayMode,
                    DelayInMillisec,
                    _currentSimToken,
                    () => IsSolverCanceled,
                    p => { },
                    m => { },
                    rows => { countOnly++; return false; }
                ));
                totalCount = countOnly;
            }
            else
            {
                totalCount = count;
            }
        }
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        if (EnableEvents && expectedTotal >0)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllParallel(int splitDepth) => RunAllUnified(true, splitDepth);
    private void RunAllSequential() => RunAllUnified(false, ParallelRootSplitDepth);
}
