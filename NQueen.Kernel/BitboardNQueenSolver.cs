using System;
using System.Threading.Tasks;

namespace NQueen.Kernel;

public static class BitboardNQueenSolver
{
    // Count solutions with symmetry reduction and optional top-level parallelization.
    public static long CountSolutions(int n, bool parallel = true)
    {
        if (n < 1 || n > 32) // fits in 64-bit, but keep diagonals simple and fast
            throw new ArgumentOutOfRangeException(nameof(n));

        ulong mask = (n == 64) ? ulong.MaxValue : ((1UL << n) - 1UL);

        // Handle even/odd split for symmetry
        int half = n / 2;
        long count = 0;

        // Parallelize first-row placements over the left half
        if (parallel && half > 1)
        {
            long total = 0;
            object gate = new object();

            Parallel.For(0, half, i =>
            {
                ulong lsb = 1UL << i;
                long local = Search(n, mask, 1, lsb, (lsb << 1), (lsb >> 1));
                lock (gate) total += local;
            });

            count += total * 2; // mirror
        }
        else
        {
            for (int i = 0; i < half; i++)
            {
                ulong lsb = 1UL << i;
                count += Search(n, mask, 1, lsb, (lsb << 1), (lsb >> 1));
            }
            count *= 2;
        }

        // If odd, handle middle column without mirroring
        if ((n & 1) == 1)
        {
            int mid = half;
            ulong lsb = 1UL << mid;
            count += Search(n, mask, 1, lsb, (lsb << 1), (lsb >> 1));
        }

        return count;
    }

    // Core DFS using bit masks. Allocation-free hot path.
    private static long Search(int n, ulong mask, int row, ulong columns, ulong diag1, ulong diag2)
    {
        if (row == n)
            return 1;

        // available positions in this row
        ulong blocked = columns | diag1 | diag2;
        ulong available = (~blocked) & mask;

        long count = 0;
        while (available != 0)
        {
            // Extract least significant set bit
            ulong lsb = available & (ulong)-(long)available;
            available ^= lsb;

            // Place queen and recurse
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