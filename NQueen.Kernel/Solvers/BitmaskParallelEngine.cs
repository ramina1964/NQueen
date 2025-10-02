namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskParallelEngine
{
    public readonly record struct AllRequest(
        int BoardSize,
        bool EnableEvents,
        Action<int[]> OnSolution,
        Action<double> ReportProgress);

    public readonly record struct UniqueRequest(
        int BoardSize,
        bool EnableEvents,
        Action<int[]> OnUniqueSolution,
        Action<double> ReportProgress);

    public void RunAll(AllRequest request)
    {
        int N = request.BoardSize;
        int totalRoots = N; // unrestricted first column
        request.ReportProgress(0.0);
        int rootsCompleted = 0;

        var tasks = new List<Task>();

        for (int firstRow = 0; firstRow < N; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var rowsArr = new int[N];
                Array.Fill(rowsArr, -1);
                rowsArr[0] = fr;

                ulong bitFirst = 1UL << fr;
                ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];

                int col = 1;
                ulong remaining = ComputeAvailable(col);

                while (true)
                {
                    if (col == N)
                    {
                        var copy = (int[])rowsArr.Clone();
                        request.OnSolution(copy);
                        col--;
                        if (col <= 0) break;
                        Restore(col, out remaining);
                        continue;
                    }
                    if (remaining == 0)
                    {
                        col--;
                        if (col <= 0) break;
                        Restore(col, out remaining);
                        continue;
                    }
                    ulong bit = remaining & (ulong)-(long)remaining;
                    remaining ^= bit;
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
                    remaining = ComputeAvailable(col);
                }

                if (request.EnableEvents)
                {
                    int done = Interlocked.Increment(ref rootsCompleted);
                    double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                    request.ReportProgress(pct);
                }

                ulong ComputeAvailable(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
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
        request.ReportProgress(100.0);
    }

    public void RunUnique(UniqueRequest request)
    {
        int N = request.BoardSize;
        int totalRoots = (N + 1) / 2;
        request.ReportProgress(0.0);
        int rootsCompleted = 0;

        var tasks = new List<Task<HashSet<int[]>>>();

        for (int firstRow = 0; firstRow < totalRoots; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<int[]>(new IntArrayComparer());
                var scratchBuf = new int[N];
                var rowsArr = new int[N];
                Array.Fill(rowsArr, -1);
                rowsArr[0] = fr;

                ulong bitFirst = 1UL << fr;
                ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];

                int col = 1;
                ulong remaining = ComputeAvailable(1);

                while (true)
                {
                    if (col == N)
                    {
                        // store canonical representatives locally
                        if (SymmetryHelper.AddIfUnique(rowsArr, localUnique, scratchBuf)) { }
                        col--;
                        if (col <= 0) break;
                        Restore(col, out remaining);
                        continue;
                    }
                    if (remaining == 0)
                    {
                        col--;
                        if (col <= 0) break;
                        Restore(col, out remaining);
                        continue;
                    }
                    ulong bit = remaining & (ulong)-(long)remaining;
                    remaining ^= bit;
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
                    remaining = ComputeAvailable(col);
                }

                if (request.EnableEvents)
                {
                    int done = Interlocked.Increment(ref rootsCompleted);
                    double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                    request.ReportProgress(pct);
                }

                return localUnique;

                ulong ComputeAvailable(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = (c == 1)
                        ? (((N & 1) == 1 && rowsArr[0] == N / 2) ? N / 2 : N)
                        : N;
                    if (maxRow < N)
                        avail &= (1UL << maxRow) - 1UL;
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

        var globalUnique = new HashSet<int[]>(new IntArrayComparer());
        var globalScratchBuf = new int[N];
        foreach (var t in tasks)
        {
            foreach (var sol in t.Result)
            {
                if (SymmetryHelper.AddIfUnique(sol, globalUnique, globalScratchBuf))
                {
                    var copy = (int[])sol.Clone();
                    request.OnUniqueSolution(copy);
                }
            }
        }

        request.ReportProgress(100.0);
    }
}
