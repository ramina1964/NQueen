namespace NQueen.Kernel.Solvers;

internal static class UniqueSolutionCounter
{
    // Counts fundamental (unique) solutions using canonical symmetry key aggregation.
    // Parallelizes over first column placements; each task enumerates full solutions for its root and adds canonical keys.
    public static ulong Count(int boardSize, Action<double>? progress, Guid token, EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender)
    {
        if (boardSize <= 0) return 0;
        int progressCounter = 0;
        // Symmetry reduction for small boards
        if (boardSize <= 8)
        {
            int halfN = (boardSize + 1) / 2;
            var uniqueMinKeys = new ConcurrentDictionary<UInt128, byte>();
            Parallel.For(0, halfN, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, firstRow =>
            {
                // Allocate correct scratch size (8 * N) for canonical transforms
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
                CountUniqueForRoot(boardSize, firstRow, uniqueMinKeys, scratchBuf);
                int done = Interlocked.Increment(ref progressCounter);
                double pct = (double)done / halfN * 100.0;
                if (progress != null) progress(pct);
                else if (progressEventSource != null && sender != null)
                    progressEventSource(sender, new ProgressUpdateEventArgs(pct, token));
            });
            ulong count = (ulong)uniqueMinKeys.Count * 2UL;
            // For odd N, handle center root separately
            if ((boardSize & 1) == 1)
            {
                var centerKeys = new ConcurrentDictionary<UInt128, byte>();
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
                CountUniqueForRoot(boardSize, boardSize / 2, centerKeys, scratchBuf);
                count += (ulong)centerKeys.Count;
            }
            if (progress == null && progressEventSource != null && sender != null)
                progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
            return count;
        }
        else
        {
            var uniqueMinKeys = new ConcurrentDictionary<UInt128, byte>();
            Parallel.For(0, boardSize, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, firstRow =>
            {
                var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
                CountUniqueForRoot(boardSize, firstRow, uniqueMinKeys, scratchBuf);
                int done = Interlocked.Increment(ref progressCounter);
                double pct = (double)done / boardSize * 100.0;
                if (progress != null) progress(pct);
                else if (progressEventSource != null && sender != null)
                    progressEventSource(sender, new ProgressUpdateEventArgs(pct, token));
            });
            if (progress == null && progressEventSource != null && sender != null)
                progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
            return (ulong)uniqueMinKeys.Count;
        }
    }

    private static void CountUniqueForRoot(int N, int firstRow, ConcurrentDictionary<UInt128, byte> uniqueMinKeys, int[] scratchBuf)
    {
        var rowsArr = new int[N];
        Array.Fill(rowsArr, -1);
        rowsArr[0] = firstRow;

        ulong bitFirst = 1UL << firstRow;
        ulong cols = bitFirst;
        ulong d1 = bitFirst << 1;
        ulong d2 = bitFirst >> 1;
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackRemaining = new ulong[N];

        int col = 1;
        ulong remaining = ~(cols | d1 | d2) & mask;

        while (true)
        {
            if (col == N)
            {
                UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out _);
                uniqueMinKeys.TryAdd(key, 0);
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
            remaining = ~(cols | d1 | d2) & mask;
        }

        void Restore(int c, out ulong rem)
        {
            rem = stackRemaining[c];
            cols = stackCols[c];
            d1 = stackD1[c];
            d2 = stackD2[c];
        }
    }
}
