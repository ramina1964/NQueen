namespace NQueen.Kernel.Solvers;

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

            if (n >= 14)
            {
                // For N >= 14, generate depth-2 work items (one item per valid (col-0, col-1)
                // queen pair with col-0 row restricted to the first half). For N=20 this yields
                // ~180 items instead of 10, giving far better core saturation and load-balancing.
                var items = BuildDepth2WorkItems(n, mask, half);
                // Wrap the array in a chunk-of-1 partitioner so Parallel.ForEach dispatches
                // one item at a time per worker instead of statically range-partitioning the
                // array up front. Work-item cost varies by orders of magnitude (centre-row
                // first queens produce vastly more subtree work than edge-row ones), so the
                // default static partitioning leaves stragglers and idle cores at the tail.
                var partitioner = Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering);
                Parallel.ForEach(
                    partitioner,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    () => 0L,
                    (item, _, local) => local + Search(n, mask, 2, item.Cols, item.D1, item.D2),
                    local => Interlocked.Add(ref total, local));
            }
            else
            {
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
            }

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

    // Enumerates all valid placements of the first two queens, restricting the first queen
    // to rows [0, half) for half-board symmetry. Returns bitmask state ready for column 2.
    private static (ulong Cols, ulong D1, ulong D2)[] BuildDepth2WorkItems(int n, ulong mask, int half)
    {
        var items = new List<(ulong Cols, ulong D1, ulong D2)>(half * (n - 2));
        for (int row0 = 0; row0 < half; row0++)
        {
            ulong bit0 = 1UL << row0;
            ulong cols0 = bit0, d1_0 = bit0 << 1, d2_0 = bit0 >> 1;
            ulong avail1 = ~(cols0 | d1_0 | d2_0) & mask;
            while (avail1 != 0)
            {
                ulong bit1 = avail1 & (ulong)-(long)avail1;
                avail1 ^= bit1;
                items.Add((cols0 | bit1, (d1_0 | bit1) << 1, (d2_0 | bit1) >> 1));
            }
        }
        return items.ToArray();
    }

    // Core DFS using bit masks. Allocation-free hot path.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static long Search(int n, ulong mask, int row, ulong columns, ulong diag1, ulong diag2)
    {
        if (row == n)
            return 1;

        ulong available = ~(columns | diag1 | diag2) & mask;

        long count = 0;
        while (available != 0)
        {
            // Extract least significant set bit and clear it (branchless)
            ulong bit = available & (ulong)-(long)available;
            available &= (available - 1);

            count += Search(
                n,
                mask,
                row + 1,
                columns | bit,
                (diag1 | bit) << 1,
                (diag2 | bit) >> 1);
        }

        return count;
    }
}
