namespace NQueen.Kernel.Solvers;

using NQueen.Domain.Utils;

public partial class BitmaskSolver
{
    private void RunAllParallel(int splitDepth)
    {
        int N = BoardSize;
        ulong totalCount = 0;
        ulong expectedTotal = SolutionCounts.GetAll(N); // 0 if unknown => fallback root progress only
        int lastPct = -1;
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        int limit = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
            BoardSize,
            splitDepth,
            EnableEvents,
            rows =>
            {
                // Central validation
                if (!ValidateRows(rows)) return;
                totalCount++;
                if (limit <= 0 || solutions.Count < limit)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                }
                // Solution-based progress (preferred when expectedTotal available)
                if (EnableEvents && expectedTotal > 0)
                {
                    int pct = (int)Math.Min(100.0, (double)totalCount / expectedTotal * 100.0);
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                    }
                }
                // Always continue search (void callback)
            },
            // Root progress fallback only if expected total unknown
            pct =>
            {
                if (EnableEvents && expectedTotal == 0)
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
            }
        ));
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        // Ensure final 100% reported if we used solution-based progress
        if (EnableEvents && expectedTotal > 0 && lastPct < 100)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllSequential()
    {
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        ulong totalCount = 0;
        ulong expectedTotal = SolutionCounts.GetAll(BoardSize);
        int lastPct = -1;
        int limit = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => { if (EnableEvents && expectedTotal == 0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                if (ValidateRows(rows) == false)
                    return false; // skip malformed
                totalCount++;
                if (limit <= 0 || solutions.Count < limit)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                }
                if (EnableEvents && expectedTotal > 0)
                {
                    int pct = (int)Math.Min(100.0, (double)totalCount / expectedTotal * 100.0);
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                    }
                }
                return false; // Always continue search
            }
        ));
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
        if (EnableEvents && expectedTotal > 0 && lastPct < 100)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void SolveAllCountOnlyMode()
    {
        ulong expectedTotal = SolutionCounts.GetAll(BoardSize);
        if (UseParallel)
        {
            // Parallel count-only path: we rely on root progress if expected total unknown, else we synthesize solution-based progress after completion (cannot update mid-task without engine changes).
            ulong count = 0;
            _parallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                BoardSize,
                UseAdaptiveDepth ? -1 : ParallelRootSplitDepth,
                c => count = c,
                pct =>
                {
                    if (EnableEvents && expectedTotal == 0)
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                }
            ));
            _solutionCount = count;
            _solutions.Clear();
            if (EnableEvents && expectedTotal > 0)
            {
                double pct = expectedTotal == 0 ? 100.0 : Math.Min(100.0, (double)count / expectedTotal * 100.0);
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
            }
        }
        else
        {
            ulong count = 0;
            int lastPct = -1;
            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents && expectedTotal == 0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    count++;
                    if (EnableEvents && expectedTotal > 0)
                    {
                        int pct = (int)Math.Min(100.0, (double)count / expectedTotal * 100.0);
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
            if (EnableEvents && expectedTotal > 0 && lastPct < 100)
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
        }
    }
}
