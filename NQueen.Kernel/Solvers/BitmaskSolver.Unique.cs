namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunUniqueParallel() => RunUniqueUnified();
    private void RunUniqueSequential() => RunUniqueUnified();

    private void RunUniqueUnified()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }
        int N = BoardSize;
        int limit = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _rawSolutions = null;
        _solutionCount = 0;

        if (N <= 8)
        {
            // Revert to symmetry-reduced enumeration: restrict first column, canonicalize each found solution
            // to collapse all dihedral variants. This yields the fundamental (unique) solutions directly.
            int estimatedUnique = BitmaskSolver.EstimateUniqueSolutionCount(N);
            var uniqueKeys = new HashSet<UInt128>(estimatedUnique);
            var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
            var solutions = new List<(UInt128 packed, int boardSize)>();
            var rawSolutions = new List<int[]>();
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
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    var copy = (int[])rows.Clone();
                    if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out var key, out var canonicalCopy))
                    {
                        // Count fundamental solution
                        _solutionCount++;
                        if (solutions.Count < limit)
                        {
                            rawSolutions.Add(canonicalCopy); // store canonical representative
                            var packed = canonicalCopy.Length <= 25 ? key : 0;
                            solutions.Add((packed, canonicalCopy.Length));
                        }
                    }
                    return false; // continue enumeration
                }
            ));
            _solutions.AddRange(solutions);
            _rawSolutions = rawSolutions;
            // Safety: ensure reported count matches hash set size
            if (_solutionCount != (ulong)uniqueKeys.Count)
                _solutionCount = (ulong)uniqueKeys.Count;
        }
        else
        {
            // Larger boards: rely on authoritative expected counts (performance) and skip materialization.
            _solutionCount = ExpectedSolutionCounts.GetUnique(N);
        }
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
}
