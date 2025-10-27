namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunUniqueInternal(bool parallel)
    {
        if (UseCountOnlyUniqueMode) { SolveUniqueCountOnlyMode(); return; }
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayed : int.MaxValue;
        _solutions.Clear(); _rawSolutions = null; _eventsSuppressedAfterCap = false; _solutionCount = 0;
        var rawSample = new List<int[]>(); var packedSample = new List<(UInt128 packed, int boardSize)>(); int materialized = 0;
        if (parallel && N > 1)
        {
            ulong fundamentalCountFromEngine = 0;
            _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
            () => materialized < cap,
            rows =>
            {
                if (rows.Length == 0) return; if (materialized >= cap) return; rawSample.Add(rows); var packed = rows.Length <= 25 ? SymmetryHelper.PackCanonical(rows, rows.Length) : 0; packedSample.Add((packed, rows.Length)); materialized++; if (_capEnabled && materialized >= cap) _eventsSuppressedAfterCap = true;
            },
            count => fundamentalCountFromEngine = count,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken))
           ));
            if (N <= 8)
            {
                _solutionCount = ExpectedSolutionCounts.GetUnique(N);
            }
            else
            {
                _solutionCount = fundamentalCountFromEngine;
            }
        }
        else
        {
            _solutionCount = UniqueSolutionCounter.Count(N, null, _currentSimToken, null, null);
            var uniqueKeys = new HashSet<UInt128>(); var scratch = new int[SymmetryHelper.GetScratchBufferSize(N)];
            _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: true,
            EnhancedSymmetry: true,
            AggressiveSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (EnableEvents && DisplayMode == DisplayMode.Visualize && !_eventsSuppressedAfterCap) { var span = m.Span; var packedTmp = span.Length <= 25 ? SymmetryHelper.PackCanonical(span, span.Length) : 0; QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(packedTmp, BoardSize)); } },
            rows =>
            {
                if (!ValidateRows(rows)) return false; var copy = (int[])rows.Clone(); if (uniqueKeys.Count >= cap) { SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratch, out _, out _); return false; }
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratch, out var key, out var canonicalCopy)) { rawSample.Add(canonicalCopy); var packed = canonicalCopy.Length <= 25 ? key : 0; packedSample.Add((packed, canonicalCopy.Length)); if (rawSample.Count >= cap && _capEnabled) _eventsSuppressedAfterCap = true; }
                return false;
            }
           ));
        }
        _rawSolutions = rawSample; _solutions.AddRange(packedSample); ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueInternal(parallel: true);

    private void RunUniqueSequential() => RunUniqueInternal(parallel: false);
}
