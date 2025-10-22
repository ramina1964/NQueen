namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllParallel(int splitDepth)
    {
        int N = BoardSize;
        ulong expectedTotal = ExpectedSolutionCounts.GetAll(N); //0 if unknown => fallback to root progress only
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        int materializeLimit = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        object lockObj = new();
        ulong totalCount =0; // final count (set by completion callback)
        int lastPct = -1;

        try
        {
            _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
                BoardSize,
                splitDepth,
                EnableEvents,
                materializeLimit,
                rows =>
                {
                    if (!ValidateRows(rows)) return;
                    lock (lockObj)
                    {
                        if (solutions.Count < materializeLimit)
                        {
                            rawSolutions.Add(rows);
                            solutions.Add((0, rows.Length));
                        }
                    }
                    if (EnableEvents && expectedTotal >0)
                    {
                        // We rely on completion for accurate count; here we approximate using materialized subset.
                        int pctApprox = (int)Math.Min(100.0, (double)solutions.Count / expectedTotal *100.0);
                        if (pctApprox != lastPct)
                        {
                            lastPct = pctApprox;
                            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pctApprox, _currentSimToken));
                        }
                    }
                },
                completed =>
                {
                    totalCount = completed; // store final total
                },
                pct =>
                {
                    if (EnableEvents && expectedTotal ==0)
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                }
            ));
        }
        catch (AggregateException ae)
        {
            // Flatten and rethrow first meaningful exception to avoid silent process termination.
            var first = ae.Flatten().InnerExceptions.FirstOrDefault();
            throw first ?? ae;
        }
        catch (Exception)
        {
            throw; // let caller handle (tests / UI)
        }
        _solutionCount = totalCount; // accurate total
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        if (EnableEvents && expectedTotal >0)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllSequential()
    {
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        ulong totalCount =0;
        ulong expectedTotal = ExpectedSolutionCounts.GetAll(BoardSize);
        int lastPct = -1;
        int limit = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => { if (EnableEvents && expectedTotal ==0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                if (!ValidateRows(rows)) return false;
                totalCount++;
                if (limit <=0 || solutions.Count < limit)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                }
                if (EnableEvents && expectedTotal >0)
                {
                    int pct = (int)Math.Min(100.0, (double)totalCount / expectedTotal *100.0);
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                    }
                }
                return false;
            }
        ));
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        if (EnableEvents && expectedTotal >0 && lastPct <100)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void SolveAllCountOnlyMode()
    {
        ulong expectedTotal = ExpectedSolutionCounts.GetAll(BoardSize);
        if (UseParallel)
        {
            ulong count =0;
            try
            {
                _parallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                    BoardSize,
                    UseAdaptiveDepth ? -1 : ParallelRootSplitDepth,
                    c => count = c,
                    pct =>
                    {
                        if (EnableEvents && expectedTotal ==0)
                            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                    }
                ));
            }
            catch (AggregateException ae)
            {
                var first = ae.Flatten().InnerExceptions.FirstOrDefault();
                throw first ?? ae;
            }
            _solutionCount = count;
            _solutions.Clear();
            if (EnableEvents && expectedTotal >0)
            {
                double pct = expectedTotal ==0 ?100.0 : Math.Min(100.0, (double)count / expectedTotal *100.0);
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
            }
        }
        else
        {
            ulong count =0;
            int lastPct = -1;
            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents && expectedTotal ==0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
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
            _solutionCount = count;
            _solutions.Clear();
            if (EnableEvents && expectedTotal >0 && lastPct <100)
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
        }
    }
}
