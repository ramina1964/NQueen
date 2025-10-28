namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private const int AggressiveSymmetryThreshold = 12;

    // Shared unique solution search core
    private void RunUniqueUnified(bool parallel)
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear(); _rawSolutions = null; _eventsSuppressedAfterCap = false; _solutionCount =0;
        var rawSample = new List<int[]>();
        var packedSample = new List<(UInt128 packed, int boardSize)>();
        int materialized =0;
        int capReachedFlag =0; //0 = not reached,1 = reached
        object lockObj = new object();
        var uniqueKeys = new HashSet<UInt128>();
        ulong fundamentalCountFromEngine =0; // track for parallel path

        Action<int[]> onUniqueSolution = rows =>
        {
            if (System.Threading.Volatile.Read(ref capReachedFlag) ==1) return;
            lock (lockObj)
            {
                if (System.Threading.Volatile.Read(ref capReachedFlag) ==1) return;
                var scratch = new int[SymmetryHelper.GetScratchBufferSize(N)];
                if (SymmetryHelper.AddIfUniquePacked(rows, uniqueKeys, scratch, out var key, out var canonicalCopy))
                {
                    // Always materialize at least one unique solution if any exist
                    int minCap = Math.Max(1, cap);
                    if (materialized < minCap)
                    {
                        rawSample.Add(canonicalCopy);
                        var packed = canonicalCopy.Length <=25 ? key :0;
                        packedSample.Add((packed, canonicalCopy.Length));
                        materialized++;
                        if (_capEnabled && materialized >= cap)
                        {
                            _eventsSuppressedAfterCap = true;
                            System.Threading.Volatile.Write(ref capReachedFlag,1);
                        }
                    }
                }
            }
        };

        if (parallel && N >1)
        {
            BitmaskParallelEngine.RunUniqueUnified(
            BoardSize,
            EnableEvents,
            cap,
            onUniqueSolution,
            count => fundamentalCountFromEngine = count,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            () => System.Threading.Volatile.Read(ref capReachedFlag) ==1
           );
            // Assign the authoritative fundamental count from parallel engine
            _solutionCount = fundamentalCountFromEngine;
        }
        else
        {
            // Sequential version
            var scratch = new int[SymmetryHelper.GetScratchBufferSize(N)];
            for (int root =0; root < N && System.Threading.Volatile.Read(ref capReachedFlag) ==0; root++)
            {
                BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: true,
                EnhancedSymmetry: true,
                AggressiveSymmetry: BoardSize >= AggressiveSymmetryThreshold,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled || System.Threading.Volatile.Read(ref capReachedFlag) ==1,
                p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                m => { if (EnableEvents && DisplayMode == DisplayMode.Visualize && !_eventsSuppressedAfterCap) { var span = m.Span; var packedTmp = span.Length <=25 ? SymmetryHelper.PackCanonical(span, span.Length) :0; QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(packedTmp, BoardSize)); } },
                rows =>
                {
                    if (System.Threading.Volatile.Read(ref capReachedFlag) ==1) return true;
                    if (!ValidateRows(rows)) return false;
                    var copy = (int[])rows.Clone();
                    if (System.Threading.Volatile.Read(ref capReachedFlag) ==0)
                    {
                        if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratch, out var key, out var canonicalCopy))
                        {
                            if (materialized < Math.Max(1, cap))
                            {
                                rawSample.Add(canonicalCopy);
                                var packed = canonicalCopy.Length <=25 ? key :0;
                                packedSample.Add((packed, canonicalCopy.Length));
                                materialized++;
                                if (materialized >= cap && _capEnabled)
                                {
                                    _eventsSuppressedAfterCap = true;
                                    System.Threading.Volatile.Write(ref capReachedFlag,1);
                                    return true; // stop current root enumeration (materialization only)
                                }
                            }
                        }
                    }
                    return false;
                }
               ));
            }
            // Assign the fundamental count from uniqueKeys (hash set holds canonical keys)
            _solutionCount = (ulong)uniqueKeys.Count;
        }
        _rawSolutions = rawSample;
        _solutions.AddRange(packedSample);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);
}
