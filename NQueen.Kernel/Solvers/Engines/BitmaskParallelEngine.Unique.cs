namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public void RunUnique(UniqueRequest request)
    {
        int N = request.BoardSize; request.ReportProgress(0.0);
        // Enumerate half roots (symmetry) like sequential unique path; center handled separately for odd N.
        int halfRoots = (N + 1) / 2; // includes center index if odd
        int rootsCompleted = 0;
        var globalUnique = new ConcurrentDictionary<UInt128, byte>();
        var tasks = new List<Task>();
        int materializedCount = 0;
        int cap = (N <= 8) ? int.MaxValue : (request.ShouldMaterialize() ? SimulationSettings.MaxDisplayedCount : 0);
        bool capReached = cap == 0;

        foreach (int fr in Enumerable.Range(0, halfRoots))
        {
            tasks.Add(Task.Run(() =>
            {
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
                var rowsArr = new int[N];
                Array.Fill(rowsArr, -1); rowsArr[0] = fr;
                ulong bitFirst = 1UL << fr; ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                ulong mask = (N == 64)
                    ? ulong.MaxValue
                    : ((1UL << N) - 1UL);

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];
                int col = 1;
                ulong remaining = ComputeAvail(col);

                while (true)
                {
                    if (col == N)
                    {
                        UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out var canonicalSpan);
                        if (globalUnique.TryAdd(key, 0))
                        {
                            bool isCenter = (N & 1) == 1 && fr == N / 2; // odd board center root
                            int multiplicity = isCenter ? 1 : 2; // paired symmetric solutions or single center
                            for (int m = 0; m < multiplicity; m++)
                            {
                                bool shouldMaterialize = !capReached && materializedCount < cap && m == 0; // only materialize once per pair
                                if (shouldMaterialize)
                                {
                                    int newVal = Interlocked.Increment(ref materializedCount);
                                    if (newVal > cap)
                                    {
                                        capReached = true;
                                        request.OnUniqueSolution(Array.Empty<int>()); // count-only sentinel
                                    }
                                    else
                                    {
                                        int[] canonicalRows = new int[N];
                                        canonicalSpan.CopyTo(canonicalRows);
                                        request.OnUniqueSolution(canonicalRows);
                                    }
                                }
                                else
                                {
                                    request.OnUniqueSolution(Array.Empty<int>()); // count-only sentinel for second of pair or after cap
                                }
                            }
                        }
                        col--; if (col <= 0) break; Restore(col, out remaining); continue;
                    }
                    if (remaining == 0)
                    {
                        col--; if (col <= 0) break; Restore(col, out remaining); continue;
                    }
                    ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit;
                    int row = BitOperations.TrailingZeroCount(bit);
                    rowsArr[col] = row;
                    stackCols[col] = cols;
                    stackD1[col] = d1;
                    stackD2[col] = d2;
                    stackRemaining[col] = remaining;
                    cols |= bit;
                    d1 = (d1 | bit) << 1;
                    d2 = (d2 | bit) >> 1;
                    col++;
                    if (col == N) continue;
                    remaining = ComputeAvail(col);
                }
                if (request.EnableEvents)
                {
                    int done = Interlocked.Increment(ref rootsCompleted);
                    double pct = Math.Min(100.0, (double)done / halfRoots * 100.0);
                    request.ReportProgress(pct);
                }
                ulong ComputeAvail(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = SymmetryHelper.MaxRowExclusiveForColumn(N, c, rowsArr);
                    if (maxRow < N)
                        avail &= (1UL << maxRow) - 1UL;
                    // Apply advanced second-column pruning only for larger boards (N > 8)
                    if (N > 8)
                        avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(N, c, rowsArr, avail);
                    return avail;
                }
                void Restore(int c, out ulong rem)
                {
                    rem = stackRemaining[c];
                    cols = stackCols[c];
                    d1 = stackD1[c];
                    d2 = stackD2[c];
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        // Final progress update
        request.ReportProgress(100.0);
    }
}
