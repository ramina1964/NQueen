namespace NQueen.Kernel;

public static class BitboardNQueenSolver
{
    // Count solutions with symmetry reduction and optional top-level parallelization.
    public static long CountSolutions(int n, bool parallel = true)
    {
        if (n < 1 || n > 32)
            throw new ArgumentOutOfRangeException(nameof(n));

        ulong mask = (1UL << n) - 1UL;

        int half = n / 2;
        long count = 0;

        if (parallel && half > 1)
        {
            long total = 0;

            Parallel.For<long>(
                fromInclusive: 0,
                toExclusive: half,
                localInit: static () => 0L,
                body: (i, state, local) =>
                {
                    ulong lsb = 1UL << i;
                    local += Search(n, mask, 1, lsb, lsb << 1, lsb >> 1);
                    return local;
                },
                localFinally: local => Interlocked.Add(ref total, local));

            count += total * 2;
        }
        else
        {
            for (int i = 0; i < half; i++)
            {
                ulong lsb = 1UL << i;
                count += Search(n, mask, 1, lsb, lsb << 1, lsb >> 1);
            }
            count *= 2;
        }

        if ((n & 1) == 1)
        {
            int mid = half;
            ulong lsb = 1UL << mid;
            count += Search(n, mask, 1, lsb, lsb << 1, lsb >> 1);
        }

        return count;
    }

    // Core DFS using bit masks. Allocation-free hot path.
    private static long Search(int n, ulong mask, int row, ulong columns, ulong diag1, ulong diag2)
    {
        if (row == n)
            return 1;

        ulong blocked = columns | diag1 | diag2;
        ulong available = (~blocked) & mask;

        long count = 0;
        while (available != 0)
        {
            // Extract least significant set bit without signed casts
            ulong lsb = available & (~available + 1);
            available ^= lsb;

            count += Search(
                n,
                mask,
                row + 1,
                columns | lsb,
                (diag1 | lsb) << 1,
                (diag2 | lsb) >> 1);
        }

        return count;
    }
}