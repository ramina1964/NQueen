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
            ulong fundamentalCountFromEngine =0;
            BitmaskParallelEngine.RunUniqueUnified(
            BoardSize,
            EnableEvents,
            cap,
            onUniqueSolution,
            count => fundamentalCountFromEngine = count,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            () => System.Threading.Volatile.Read(ref capReachedFlag) ==1
           );
            // After cap, continue to enumerate and count unique solutions (no lookup)
            if (materialized >= cap)
            {
                // Enumerate all unique solutions to count, but do not materialize more
                var globalUnique = new HashSet<UInt128>();
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
                var rowsArr = new int[N];
                for (int root =0; root < N; root++)
                {
                    Array.Fill(rowsArr, -1);
                    rowsArr[0] = root;
                    ulong bitFirst =1UL << root;
                    ulong cols = bitFirst;
                    ulong d1 = bitFirst <<1;
                    ulong d2 = bitFirst >>1;
                    ulong mask = (N ==64) ? ulong.MaxValue : ((1UL << N) -1UL);
                    ulong[] stackCols = new ulong[N];
                    ulong[] stackD1 = new ulong[N];
                    ulong[] stackD2 = new ulong[N];
                    ulong[] stackRemaining = new ulong[N];
                    int col =1;
                    ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out _);
                            globalUnique.Add(key);
                            col--; if (col <=0) break;
                            Restore(col, out remaining); continue;
                        }
                        if (remaining ==0)
                        {
                            col--; if (col <=0) break;
                            Restore(col, out remaining); continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit;
                        int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row;
                        stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) <<1; d2 = (d2 | bit) >>1;
                        col++;
                        if (col == N) continue;
                        remaining = ~(cols | d1 | d2) & mask;
                    }
                    void Restore(int c, out ulong rem)
                    {
                        rem = stackRemaining[c];
                        cols = stackCols[c]; d1 = stackD1[c]; d2 = stackD2[c];
                    }
                }
                _solutionCount = (ulong)globalUnique.Count;
            }
            else
            {
                _solutionCount = (ulong)packedSample.Count;
            }
        }
        else
        {
            // Sequential version
            var scratch = new int[SymmetryHelper.GetScratchBufferSize(N)];
            int counted =0;
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
                        if (uniqueKeys.Count < cap)
                        {
                            if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratch, out var key, out var canonicalCopy))
                            {
                                rawSample.Add(canonicalCopy);
                                var packed = canonicalCopy.Length <=25 ? key :0;
                                packedSample.Add((packed, canonicalCopy.Length));
                                materialized++;
                                if (materialized >= cap && _capEnabled)
                                {
                                    _eventsSuppressedAfterCap = true;
                                    System.Threading.Volatile.Write(ref capReachedFlag,1);
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
               ));
            }
            // After cap, enumerate all unique solutions to count, but do not materialize more
            if (materialized >= cap)
            {
                var globalUnique = new HashSet<UInt128>();
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
                var rowsArr = new int[N];
                for (int root =0; root < N; root++)
                {
                    Array.Fill(rowsArr, -1);
                    rowsArr[0] = root;
                    ulong bitFirst =1UL << root;
                    ulong cols = bitFirst;
                    ulong d1 = bitFirst <<1;
                    ulong d2 = bitFirst >>1;
                    ulong mask = (N ==64) ? ulong.MaxValue : ((1UL << N) -1UL);
                    ulong[] stackCols = new ulong[N];
                    ulong[] stackD1 = new ulong[N];
                    ulong[] stackD2 = new ulong[N];
                    ulong[] stackRemaining = new ulong[N];
                    int col =1;
                    ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out _);
                            globalUnique.Add(key);
                            col--; if (col <=0) break;
                            Restore(col, out remaining); continue;
                        }
                        if (remaining ==0)
                        {
                            col--; if (col <=0) break;
                            Restore(col, out remaining); continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit;
                        int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row;
                        stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) <<1; d2 = (d2 | bit) >>1;
                        col++;
                        if (col == N) continue;
                        remaining = ~(cols | d1 | d2) & mask;
                    }
                    void Restore(int c, out ulong rem)
                    {
                        rem = stackRemaining[c];
                        cols = stackCols[c]; d1 = stackD1[c]; d2 = stackD2[c];
                    }
                }
                _solutionCount = (ulong)globalUnique.Count;
            }
            else
            {
                _solutionCount = (ulong)packedSample.Count;
            }
        }
        _rawSolutions = rawSample;
        _solutions.AddRange(packedSample);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);
}
