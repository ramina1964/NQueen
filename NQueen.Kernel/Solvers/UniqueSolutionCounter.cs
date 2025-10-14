namespace NQueen.Kernel.Solvers.Counters;

internal static class UniqueSolutionCounter
{
    public static ulong Count(int boardSize, Action<double>? progress, Guid token, EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender)
    {
        if (boardSize <= 0) return 0;
        ulong total = 0;
        int half = boardSize / 2;
        int totalSteps = half + ((boardSize & 1) == 1 ? 1 : 0);
        int progressCounter = 0;
        object lockObj = new();

        Parallel.For(0, half, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, firstRow =>
        {
            ulong localCount = CountUniqueForRoot(boardSize, firstRow) * 2; // mirror
            lock (lockObj)
            {
                total += localCount;
                progressCounter++;
                double pct = (double)progressCounter / totalSteps * 100.0;
                if (progress != null) progress(pct);
                else if (progressEventSource != null && sender != null)
                    progressEventSource(sender, new ProgressUpdateEventArgs(pct, token));
            }
        });

        if ((boardSize & 1) == 1)
        {
            total += CountUniqueForRoot(boardSize, half); // center row (no mirror)
            if (progress != null) progress(100.0);
            else if (progressEventSource != null && sender != null)
                progressEventSource(sender, new ProgressUpdateEventArgs(100.0, token));
        }
        return total;
    }

    private static ulong CountUniqueForRoot(int N, int firstRow)
    {
        ulong count = 0;
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
                count++;
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
        return count;
    }
}
