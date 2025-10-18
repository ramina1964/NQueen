namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public void RunUnique(UniqueRequest request)
    {
        int N = request.BoardSize; request.ReportProgress(0.0);
        int totalRoots = (N + 1) / 2;
        int rootsCompleted = 0;
        var globalUnique = new HashSet<UInt128>();
        var globalLock = new object();
        var tasks = new List<Task<ulong>>();
        // Global materialized counter (shared across tasks)
        int materializedCount = 0;
        // Cap only applies for N > 8 (small boards fully materialized); when ShouldMaterialize returns false treat as count-only.
        int cap = (N <= 8) ? int.MaxValue : (request.ShouldMaterialize() ? SimulationSettings.MaxNoOfSolutionsInOutput : 0);

        foreach (int fr in Enumerable.Range(0, totalRoots))
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
                ulong localCount = 0; // count of non-materialized unique solutions for this task
                bool capReached = cap == 0; // if cap == 0 we are in count-only mode from start

                while (true)
                {
                    if (col == N)
                    {
                        UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out var canonicalSpan);
                        bool isUnique;
                        lock (globalLock)
                        {
                            isUnique = globalUnique.Add(key);
                        }
                        if (isUnique)
                        {
                            // Decide materialization for this unique solution.
                            bool shouldMaterialize = !capReached && materializedCount < cap;
                            if (shouldMaterialize)
                            {
                                // Atomically increment global materialized count; re-check against cap.
                                int newVal = Interlocked.Increment(ref materializedCount);
                                if (newVal > cap)
                                {
                                    // We crossed the cap boundary with this solution -> treat as count-only.
                                    capReached = true;
                                    shouldMaterialize = false;
                                    localCount++; // count-only solution
                                }
                                else
                                {
                                    // Materialize canonical rows
                                    int[] canonicalRows = new int[N];
                                    canonicalSpan.CopyTo(canonicalRows);
                                    request.OnUniqueSolution(canonicalRows);
                                }
                            }
                            else
                            {
                                // Count-only path: still invoke callback so solver can increment total count.
                                request.OnUniqueSolution(Array.Empty<int>());
                                localCount++;
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
                    double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                    request.ReportProgress(pct);
                }
                ulong ComputeAvail(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = SymmetryHelper.MaxRowExclusiveForColumn(N, c, rowsArr);
                    if (maxRow < N)
                        avail &= (1UL << maxRow) - 1UL;
                    // Apply advanced second-column pruning only for larger boards (N > 8) to avoid regression for small N.
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
                return localCount;
            }));
        }
        Task.WaitAll(tasks.ToArray());
        // Final progress update
        request.ReportProgress(100.0);
    }
}
