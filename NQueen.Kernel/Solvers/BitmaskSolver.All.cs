namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Unified executor for All mode (materialize + count).
    private void ExecuteAllModeUnified(bool parallel, int splitDepth)
    {
        int boardSize = BoardSize;
        int cap = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;

        SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: false);

        List<(UInt128 packed, int boardSize)> solutions = [];
        List<int[]> largeRaw = [];
        ulong totalCount = 0;
        int materialized = 0;

        bool packable = boardSize <= 25;

        if (parallel && boardSize > 1)
        {
            // Parallel path via engine unified API.
            var threadLocalPacked = new ThreadLocal<List<(UInt128, int)>>(() => [], trackAllValues: true);
            var threadLocalRaw = new ThreadLocal<List<int[]>>(() => [], trackAllValues: true);
            void OnSolution(int[] rows)
            {
                if (!ValidateRows(rows)) return;
                int idx = Interlocked.Increment(ref materialized);
                if (idx <= cap)
                {
                    if (packable)
                    {
                        // Pack actual rows so UI can reconstruct queen placement for first solution
                        UInt128 packed = SymmetryHelper.PackRows(rows);
                        threadLocalPacked.Value!.Add((packed, rows.Length));
                    }
                    else
                    {
                        var copy = new int[rows.Length];
                        Array.Copy(rows, copy, rows.Length);
                        threadLocalRaw.Value!.Add(copy);
                    }
                }
            }
            ulong counted = 0;
            BitmaskParallelEngine.RunAllUnified(
                boardSize,
                splitDepth,
                EnableEvents,
                cap,
                OnSolution,
                c => counted = c,
                pct => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken)); },
                () => false);
            foreach (var list in threadLocalPacked.Values) solutions.AddRange(list);
            foreach (var list in threadLocalRaw.Values) largeRaw.AddRange(list);
            totalCount = counted;
        }
        else
        {
            // Sequential fallback using search engine.
            ulong counted = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                boardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, boardSize)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (materialized < cap)
                    {
                        if (packable)
                        {
                            UInt128 packed = SymmetryHelper.PackRows(rows);
                            solutions.Add((packed, rows.Length));
                        }
                        else
                        {
                            var copy = new int[rows.Length];
                            Array.Copy(rows, copy, rows.Length);
                            largeRaw.Add(copy);
                        }
                        materialized++;
                    }
                    counted++;
                    return false;
                }
            ));
            totalCount = counted;
        }
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _largeBoardRawSolutions.Clear();
        _largeBoardRawSolutions.AddRange(largeRaw);
        if (EnableEvents)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunAllUnified(bool isParallel, int splitDepth) => ExecuteAllModeUnified(
        isParallel, splitDepth);
    
    private void RunAllParallel(int splitDepth) => ExecuteAllModeUnified(true, splitDepth);
    
    private void RunAllSequential() => ExecuteAllModeUnified(false, ParallelRootSplitDepth);
}
