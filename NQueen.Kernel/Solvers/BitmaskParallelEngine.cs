using System.Buffers;
using System.Collections.Concurrent;

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

    private static int ChooseAdaptiveSplitDepth(int boardSize)
    {
        if (boardSize <= 10) return 1;
        if (boardSize <= 13) return 2;
        return 3;
    }

    public void RunAll(AllRequest request)
    {
        int N = request.BoardSize;
        int splitDepth = request.RootSplitDepth < 0 ? ChooseAdaptiveSplitDepth(N) : request.RootSplitDepth;
        if (splitDepth > N) splitDepth = N;

        request.ReportProgress(0.0);
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
        int workerCount = Math.Min(Environment.ProcessorCount, totalRoots);
        var queue = new ConcurrentQueue<RootFrame>(rootList);
        var solutionCount = 0UL;
        var tasks = new List<Task>(workerCount);

        for (int w = 0; w < workerCount; w++)
        {
            tasks.Add(Task.Run(() =>
            {
                var localBufferPool = ArrayPool<ulong>.Shared;
                var localIntPool = ArrayPool<int>.Shared;
                while (queue.TryDequeue(out var root))
                {
                    var rowsArr = root.Rows;
                    int startCol = root.Col;
                    for (int i = startCol; i < N; i++) if (rowsArr[i] == 0 && i >= startCol) rowsArr[i] = -1;
                    ulong cols = root.Cols;
                    ulong d1 = root.D1;
                    ulong d2 = root.D2;
                    var stackCols = localBufferPool.Rent(N);
                    var stackD1 = localBufferPool.Rent(N);
                    var stackD2 = localBufferPool.Rent(N);
                    var stackRemaining = localBufferPool.Rent(N);
                    int col = startCol;
                    ulong remaining = ComputeAvailable(col);
                    try
                    {
                        while (true)
                        {
                            if (col == N)
                            {
                                var copy = (int[])rowsArr.Clone();
                                request.OnSolution(copy);
                                Interlocked.Increment(ref solutionCount);
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
                        localBufferPool.Return(stackCols);
                        localBufferPool.Return(stackD1);
                        localBufferPool.Return(stackD2);
                        localBufferPool.Return(stackRemaining);
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
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        request.ReportProgress(100.0);
    }

    public void RunUnique(UniqueRequest request)
    {
        int N = request.BoardSize;
        // We currently only support splitDepth == 1 for unique due to symmetry constraints.
        int splitDepth = 1;
        request.ReportProgress(0.0);
        int totalRoots = (N + 1) / 2;
        int rootsCompleted = 0;

        var tasks = new List<Task<HashSet<int[]>>>();

        for (int firstRow = 0; firstRow < totalRoots; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<int[]>(new IntArrayComparer());
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

        var globalUnique = new HashSet<int[]>(new IntArrayComparer());
        var globalScratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
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

    private readonly record struct RootFrame(int Col, ulong Cols, ulong D1, ulong D2, int[] Rows);
}
