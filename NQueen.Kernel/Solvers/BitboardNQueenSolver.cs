namespace NQueen.Kernel.Solvers;

public static class BitboardNQueenSolver
{
    // Count solutions with symmetry reduction and optional top-level parallelization.
    //
    // The DFS core is the allocation-free iterative `Search` (with `stackalloc Frame[n]`
    // replacing the recursive call stack). The previously-recursive variant lives on as
    // `SearchRecursive` / `CountSolutionsRecursive` and is exposed as `internal` solely so
    // `AllCountOnlyRecursiveVsIterativeBenchmark` can A/B the two and prove the iterative
    // variant remains the faster path on this code base. See `CHANGELOG.md [Unreleased] →
    // Performance` for the A/B numbers (-3.0 % at N = 18, -3.1 % at N = 16, both with
    // non-overlapping 99.9 % CIs).
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

    // Recursive variant retained solely as the A/B regression-guard baseline for
    // AllCountOnlyRecursiveVsIterativeBenchmark. Identical control flow to CountSolutions,
    // dispatches to the recursive SearchRecursive at every site. Not called from production.
    internal static long CountSolutionsRecursive(int n, bool parallel = true)
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
                var items = BuildDepth2WorkItems(n, mask, half);
                var partitioner = Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering);
                Parallel.ForEach(
                    partitioner,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    () => 0L,
                    (item, _, local) => local + SearchRecursive(n, mask, 2, item.Cols, item.D1, item.D2),
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
                        local += SearchRecursive(n, mask, 1, lsb, lsb << 1, lsb >> 1);
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
                count += SearchRecursive(n, mask, 1, lsb, lsb << 1, lsb >> 1);
            }
            count *= 2;
        }

        if ((n & 1) == 1)
        {
            int mid = half;
            ulong lsb = 1UL << mid;
            count += SearchRecursive(n, mask, 1, lsb, lsb << 1, lsb >> 1);
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

    // Allocation-free iterative DFS using bit masks. Production hot path.
    //
    // A fixed-size on-stack frame buffer (`stackalloc Frame[n - startRow]`) replaces the
    // recursive call stack. Modelled on the iterative pattern in
    // `BitmaskSearchEngine.MainLoopCountOnly`. n is bounded above by `CountSolutions` at 32,
    // so the buffer is at most 32 * sizeof(Frame) = 1 KB on the stack.
    //
    // Includes a leaf-shortcut: when the next descent would be the terminal row, count
    // directly without pushing a frame, saving one push/pop pair per leaf solution.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static long Search(int n, ulong mask, int startRow, ulong cols0, ulong d1_0, ulong d2_0)
    {
        if (startRow == n)
            return 1;

        // The frames span depths [startRow, n); index is `row - startRow`.
        Span<Frame> stack = stackalloc Frame[n - startRow];

        long count = 0;
        int row = startRow;
        ulong cols = cols0;
        ulong d1 = d1_0;
        ulong d2 = d2_0;
        ulong available = ~(cols | d1 | d2) & mask;

        while (true)
        {
            if (available == 0)
            {
                // Backtrack one level. If we've popped past startRow, the search is done.
                row--;
                if (row < startRow) break;
                ref Frame frame = ref stack[row - startRow];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; available = frame.Remaining;
                continue;
            }

            // Extract least significant set bit and clear it (branchless).
            ulong bit = available & (ulong)-(long)available;
            available &= available - 1;

            // If the next row would be the leaf, count directly without pushing a frame —
            // saves one stack write/read pair per leaf solution.
            if (row + 1 == n)
            {
                count++;
                continue;
            }

            // Push the rest of `available` so we can resume here on backtrack.
            stack[row - startRow] = new Frame(cols, d1, d2, available);

            // Descend.
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            row++;
            available = ~(cols | d1 | d2) & mask;
        }

        return count;
    }

    // Recursive DFS variant retained solely as the A/B regression-guard baseline for
    // AllCountOnlyRecursiveVsIterativeBenchmark. Was the production hot path until
    // perf/all-mode-iterative-core swapped it for the iterative `Search` above; staying in
    // this file under `internal` access lets the benchmark continue exercising it without
    // exposing the recursive form on the public surface. Not called from production.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static long SearchRecursive(int n, ulong mask, int row, ulong columns, ulong diag1, ulong diag2)
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

            count += SearchRecursive(
                n,
                mask,
                row + 1,
                columns | bit,
                (diag1 | bit) << 1,
                (diag2 | bit) >> 1);
        }

        return count;
    }

    private readonly record struct Frame(ulong Cols, ulong D1, ulong D2, ulong Remaining);
}
