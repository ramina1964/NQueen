namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskParallelEngine
{
    public readonly record struct AllRequest(
        int BoardSize,
        bool EnableEvents,
        int RootSplitDepth,
        Action<int[]> OnSolution,
        Action<double> ReportProgress);

    public readonly record struct UniqueRequest(
        int BoardSize,
        bool EnableEvents,
        int RootSplitDepth,
        Action<int[]> OnUniqueSolution,
        Action<double> ReportProgress);

    public readonly record struct AllCountOnlyRequest(
        int BoardSize,
        int RootSplitDepth,
        Action<ulong> OnCount,
        Action<double> ReportProgress);

    public readonly record struct UniqueCountOnlyRequest(
        int BoardSize,
        int RootSplitDepth,
        Action<ulong> OnCount,
        Action<double> ReportProgress);

    public void RunAll(AllRequest request)
    {
        int N = request.BoardSize;
        int splitDepth = request.RootSplitDepth < 1 ? 1 : request.RootSplitDepth;
        if (splitDepth > N) splitDepth = N;

        // Adaptive logic: if splitDepth is -1, use heuristic
        if (request.RootSplitDepth == -1)
        {
            splitDepth = ParallelSplitDepthHeuristic.GetOptimalSplitDepth(N);
        }

        request.ReportProgress(0.0);
        var tasks = new List<Task>();

        var rootStack = new Stack<RootFrame>();
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, new int[N]));
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        var rootList = new List<RootFrame>(N * N);

        while (rootStack.Count > 0)
        {
            var frame = rootStack.Pop();
            int col = frame.Col;
            if (col == splitDepth)
            {
                rootList.Add(frame);
                continue;
            }
            ulong avail = ~(frame.Cols | frame.D1 | frame.D2) & mask;
            while (avail != 0)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                var rowsCopy = (int[])frame.Rows.Clone();
                rowsCopy[col] = row;
                ulong cols = frame.Cols | bit;
                ulong d1 = (frame.D1 | bit) << 1;
                ulong d2 = (frame.D2 | bit) >> 1;
                rootStack.Push(new RootFrame(col + 1, cols, d1, d2, rowsCopy));
            }
        }

        int totalRoots = rootList.Count;
        int rootsCompleted = 0;

        foreach (var root in rootList)
        {
            tasks.Add(Task.Run(() =>
            {
                var rowsArr = root.Rows;
                int startCol = root.Col;
                for (int i = 0; i < N; i++) if (i >= startCol && rowsArr[i] == 0) rowsArr[i] = -1;

                ulong cols = root.Cols;
                ulong d1 = root.D1;
                ulong d2 = root.D2;

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];

                int col = startCol;
                ulong remaining = ComputeAvailable(col);

                while (true)
                {
                    if (col == N)
                    {
                        var copy = (int[])rowsArr.Clone();
                        request.OnSolution(copy);
                        col--;
                        if (col < startCol) break;
                        Restore(col, out remaining);
                        continue;
                    }
                    if (remaining == 0)
                    {
                        col--;
                        if (col < startCol) break;
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
        request.ReportProgress(0.0);
        int totalRoots = (N + 1) / 2;
        int rootsCompleted = 0;

        var tasks = new List<Task<HashSet<UInt128>>>();

        for (int firstRow = 0; firstRow < totalRoots; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<UInt128>();
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
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
                        if (SymmetryHelper.AddIfUniquePacked(rowsArr, localUnique, scratchBuf, out _, out _)) { }
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
                    int maxRow = SymmetryHelper.MaxRowExclusiveForColumn(N, c, rowsArr);
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

        var globalUnique = new HashSet<UInt128>();
        var globalScratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
        foreach (var t in tasks)
        {
            foreach (var key in t.Result)
            {
                // We only need canonical arrays for materialization when OnUniqueSolution is invoked.
                if (globalUnique.Add(key))
                {
                    // Reconstruct representative array from key for callback consumers.
                    var rows = UnpackKeyToArray(key, N);
                    request.OnUniqueSolution(rows);
                }
            }
        }

        request.ReportProgress(100.0);
    }

    public void RunAllCountOnly(AllCountOnlyRequest request)
    {
        int N = request.BoardSize;
        int splitDepth = request.RootSplitDepth < 1 ? 1 : request.RootSplitDepth;
        if (splitDepth > N) splitDepth = N;
        if (request.RootSplitDepth == -1)
        {
            splitDepth = ParallelSplitDepthHeuristic.GetOptimalSplitDepth(N);
        }

        request.ReportProgress(0.0);
        var tasks = new List<Task<ulong>>();

        var rootStack = new Stack<RootFrame>();
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, new int[N]));
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        var rootList = new List<RootFrame>(N * N);

        while (rootStack.Count > 0)
        {
            var frame = rootStack.Pop();
            int col = frame.Col;
            if (col == splitDepth)
            {
                rootList.Add(frame);
                continue;
            }
            ulong avail = ~(frame.Cols | frame.D1 | frame.D2) & mask;
            while (avail != 0)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                var rowsCopy = (int[])frame.Rows.Clone();
                rowsCopy[col] = BitOperations.TrailingZeroCount(bit);
                ulong cols = frame.Cols | bit;
                ulong d1 = (frame.D1 | bit) << 1;
                ulong d2 = (frame.D2 | bit) >> 1;
                rootStack.Push(new RootFrame(col + 1, cols, d1, d2, rowsCopy));
            }
        }

        int totalRoots = rootList.Count;
        int rootsCompleted = 0;

        foreach (var root in rootList)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong localCount = 0;
                var rowsArr = root.Rows;
                int startCol = root.Col;
                for (int i = 0; i < N; i++) if (i >= startCol && rowsArr[i] == 0) rowsArr[i] = -1;

                ulong cols = root.Cols;
                ulong d1 = root.D1;
                ulong d2 = root.D2;

                // Use ArrayPool to reduce allocations for stack arrays
                var pool = System.Buffers.ArrayPool<ulong>.Shared;
                ulong[] stackCols = pool.Rent(N);
                ulong[] stackD1 = pool.Rent(N);
                ulong[] stackD2 = pool.Rent(N);
                ulong[] stackRemaining = pool.Rent(N);

                try
                {
                    int col = startCol;
                    ulong remaining = ComputeAvailable(col);

                    while (true)
                    {
                        if (col == N)
                        {
                            localCount++;
                            col--;
                            if (col < startCol) break;
                            Restore(col, out remaining);
                            continue;
                        }
                        if (remaining == 0)
                        {
                            col--;
                            if (col < startCol) break;
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
                }
                finally
                {
                    pool.Return(stackCols);
                    pool.Return(stackD1);
                    pool.Return(stackD2);
                    pool.Return(stackRemaining);
                }

                if (request.ReportProgress != null)
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

                return localCount;
            }));
        }

        Task.WaitAll(tasks.ToArray());
        ulong totalCount = 0;
        foreach (var t in tasks) totalCount += t.Result;
        request.OnCount(totalCount);
        request.ReportProgress(100.0);
    }

    public void RunUniqueCountOnly(UniqueCountOnlyRequest request)
    {
        int N = request.BoardSize;
        request.ReportProgress(0.0);
        int totalRoots = (N + 1) / 2;
        int rootsCompleted = 0;

        var tasks = new List<Task<HashSet<UInt128>>>();

        for (int firstRow = 0; firstRow < totalRoots; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<UInt128>();
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
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
                        // Only compute canonical key, do not materialize canonical array
                        var key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out _);
                        localUnique.Add(key);
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

                if (request.ReportProgress != null)
                {
                    int done = Interlocked.Increment(ref rootsCompleted);
                    double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                    request.ReportProgress(pct);
                }

                ulong ComputeAvailable(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = SymmetryHelper.MaxRowExclusiveForColumn(N, c, rowsArr);
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

                return localUnique;
            }));
        }

        Task.WaitAll(tasks.ToArray());
        var globalUnique = new HashSet<UInt128>();
        foreach (var t in tasks)
        {
            foreach (var key in t.Result)
            {
                globalUnique.Add(key);
            }
        }
        request.OnCount((ulong)globalUnique.Count);
        request.ReportProgress(100.0);
    }

    private static int[] UnpackKeyToArray(UInt128 key, int n)
    {
        var rows = new int[n];
        // Packed with most significant row first (left-shift process). Need to read back reverse.
        for (int i = n - 1; i >= 0; i--)
        {
            rows[i] = (int)(key & 0x1F); // 5 bits
            key >>= 5;
        }
        return rows;
    }

    private readonly record struct RootFrame(int Col, ulong Cols, ulong D1, ulong D2, int[] Rows);
}
