namespace NQueen.Kernel.Solvers.Engines;

public static class CanonicalUniqueSearchEngine
{
    public static ulong CountUnique(int boardSize, Action<int[]>? onSolution = null)
    {
        if (boardSize <= 0) return 0;
        int N = boardSize;
        ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int[] queenRows = new int[N];
        Array.Fill(queenRows, -1);
        int[] scratch = new int[N * 8];
        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackAvail = new ulong[N];
        ulong count = 0;
        int firstRowLimitExclusive = (N + 1) / 2;

        for (int fr = 0; fr < firstRowLimitExclusive; fr++)
        {
            Array.Fill(queenRows, -1);
            ulong bitFirst = 1UL << fr;
            ulong cols = bitFirst;
            ulong d1 = bitFirst << 1;
            ulong d2 = bitFirst >> 1;
            queenRows[0] = fr;
            int col = 1;
            ulong avail = ~(cols | d1 | d2) & fullMask;

            while (true)
            {
                if (col == N)
                {
                    if (SymmetryHelper.IsIdentityCanonical(queenRows, scratch))
                    {
                        count++;
                        if (onSolution != null)
                        {
                            var copy = new int[N];
                            Buffer.BlockCopy(queenRows, 0, copy, 0, N * sizeof(int));
                            onSolution(copy);
                        }
                    }
                    col--;
                    if (col <= 0) break;
                    Restore(col, out avail, ref cols, ref d1, ref d2, queenRows, stackAvail, stackCols, stackD1, stackD2);
                    continue;
                }
                if (avail == 0UL)
                {
                    col--;
                    if (col <= 0) break;
                    Restore(col, out avail, ref cols, ref d1, ref d2, queenRows, stackAvail, stackCols, stackD1, stackD2);
                    continue;
                }
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                queenRows[col] = row;
                stackCols[col] = cols;
                stackD1[col] = d1;
                stackD2[col] = d2;
                stackAvail[col] = avail;
                cols |= bit;
                d1 = (d1 | bit) << 1;
                d2 = (d2 | bit) >> 1;
                col++;
                if (col == N) continue;
                avail = ~(cols | d1 | d2) & fullMask;
            }
        }
        return count;
    }

    private static void Restore(int c, out ulong avail, ref ulong cols, ref ulong d1, ref ulong d2,
        int[] queenRows, ulong[] stackAvail, ulong[] stackCols, ulong[] stackD1, ulong[] stackD2)
    {
        avail = stackAvail[c];
        cols = stackCols[c];
        d1 = stackD1[c];
        d2 = stackD2[c];
        queenRows[c] = -1;
    }
}

