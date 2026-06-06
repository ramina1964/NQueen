namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Selects the fastest unique count-only algorithm for the given board size.
    // Only called for N <= LookupThresholdN-1 (20); the lookup table makes N>=21 unreachable.
    private ulong CountUniqueAdaptive(int n)
    {
        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        EnsureMinThreads();

        try
        {
            if (n >= SimulationSettings.UniqueCountOnlyParallelThresholdN)
            {
                // N = 16..20: half-board parallel DFS.
                return CountUniqueFastHalfBoard(n);
            }
            else
            {
                // N < 16: parallel canonical enumeration via BitmaskParallelEngine.
                ulong total = 0;
                BitmaskParallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest
                {
                    BoardSize = n,
                    EnableEvents = false,
                    ShouldMaterialize = () => false,
                    OnUniqueSolution = _ => { },
                    OnCompletedUniqueCount = count => total = count,
                    ReportProgress = _ => { }
                });
                return total;
            }
        }
        finally
        {
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
        }
    }

    // Fast unique count-only path: parallel half-board DFS with symmetry pruning.
    // Reliable for N >= UniqueCountOnlyParallelThresholdN (16).
    private ulong CountUniqueFastHalfBoard(int n)
    {
        if (n <= 0) return 0UL;

        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        int firstRowLimitExclusive = (n + 1) / 2;
        ulong fullMask = (n == 64) ? ulong.MaxValue : ((1UL << n) - 1UL);
        int cores = Environment.ProcessorCount;

        int pruneDepthGate = int.MaxValue;
        if (EnablePrefixMinimalityPruning || EnablePartialReflectionPruning)
        {
            if (n >= 20) pruneDepthGate = 1;
            else if (n >= SimulationSettings.PrefixPruneEarlyThresholdN) pruneDepthGate = 0;
            else if (n >= 16) pruneDepthGate = 2;
            else if (n >= SimulationSettings.LargeBoardSymmetryPruningThreshold) pruneDepthGate = 3;
        }

        var scratchPool = ArrayPool<int>.Shared;
        long total = 0L;

        // Capture the pruning flag as a local so a concurrent Configure() on the global
        // statics cannot corrupt in-flight pruning decisions for this invocation. Only the
        // reflection prune is sound for canonical counting (see ShouldPrunePrefixFull); the
        // minimality flag deliberately plays no part here.
        bool reflectionEnabled = EnablePartialReflectionPruning;

        EnsureMinThreads();
        try
        {
            // Depth-2 work-item partitioning. Instead of ~(n+1)/2 coarse, uneven root-row
            // ranges (one per first-column row), enumerate every valid (col-0, col-1) queen
            // pair with col-0 restricted to the top half. This yields ~180 fine-grained items
            // at N=20 (vs ~10), giving far better core saturation and load-balancing while
            // visiting an identical leaf set, so the canonical count is provably unchanged.
            var items = BuildUniqueDepth2WorkItems(n, fullMask, firstRowLimitExclusive);
            var po = new ParallelOptions { MaxDegreeOfParallelism = cores };

            Parallel.ForEach(
                items,
                po,
                localInit: () =>
                {
                    int[] rows = new int[n];
                    Array.Fill(rows, -1);
                    int[] scratch = scratchPool.Rent(n * 8);
                    return (rows, scratch, count: 0UL);
                },
                body: (item, _, local) =>
                {
                    // rows[2..] is restored to -1 by CountCanonicalDFS on every branch, so the
                    // per-thread buffer stays clean for reuse across items; only the first two
                    // columns need to be (re)seeded here.
                    local.rows[0] = item.Row0;
                    local.rows[1] = item.Row1;
                    local.count += CountCanonicalDFS(
                        2, item.Cols, item.D1, item.D2,
                        n, fullMask, pruneDepthGate, reflectionEnabled,
                        local.rows, local.scratch);
                    return local;
                },
                localFinally: local =>
                {
                    if (local.count != 0)
                        Interlocked.Add(ref total, (long)local.count);
                    scratchPool.Return(local.scratch, clearArray: false);
                });
        }
        finally
        {
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
        }

        return (ulong)total;
    }

    // Enumerates every valid (col-0, col-1) queen pair with col-0 restricted to the top half
    // [0, firstRowLimitExclusive) — the sound horizontal-reflection root partition that already
    // captures each canonical solution exactly once. Each item carries the two row indices (so
    // rows[] can be seeded for canonical checking) plus the bitmask state ready for column 2.
    //
    // The col-1 reflection prune is intentionally NOT applied here: for the sizes this path
    // serves (N = 16..20) it is a no-op (an in-the-top-half first row is already strictly below
    // its mirror, so ShouldPrunePrefixFull breaks at i = 0), and even if it fired it could only
    // remove non-canonical branches, never changing the leaf count that IsIdentityCanonical sees.
    private static (int Row0, int Row1, ulong Cols, ulong D1, ulong D2)[] BuildUniqueDepth2WorkItems(
        int n, ulong fullMask, int firstRowLimitExclusive)
    {
        var items = new List<(int, int, ulong, ulong, ulong)>(firstRowLimitExclusive * (n - 1));
        for (int row0 = 0; row0 < firstRowLimitExclusive; row0++)
        {
            ulong bit0 = 1UL << row0;
            ulong cols0 = bit0, d1_0 = bit0 << 1, d2_0 = bit0 >> 1;
            ulong avail1 = ~(cols0 | d1_0 | d2_0) & fullMask;
            while (avail1 != 0)
            {
                ulong bit1 = avail1 & (ulong)-(long)avail1;
                avail1 ^= bit1;
                int row1 = BitOperations.TrailingZeroCount(bit1);
                items.Add((row0, row1, cols0 | bit1, (d1_0 | bit1) << 1, (d2_0 | bit1) >> 1));
            }
        }
        return items.ToArray();
    }

    // Count-returning canonical DFS from a given column. Extracted from the former closure so it
    // can be driven by depth-2 work items in parallel (summation is commutative, so per-item
    // counts combine safely). Stateless reflection-only prefix pruning: ShouldPrunePrefixFull
    // re-scans columns 0..col each call, so gating it at col >= pruneDepthGate is safe. Only
    // horizontal reflection is a sound forward-prefix prune for canonical counting;
    // IsIdentityCanonical at the leaf is the final arbiter across all eight symmetries, so the
    // half-board root partition and the delayed gate never drop a valid canonical solution.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ulong CountCanonicalDFS(
        int col, ulong cols, ulong d1, ulong d2,
        int n, ulong fullMask, int pruneDepthGate, bool reflectionEnabled,
        int[] rows, int[] scratch)
    {
        if ((col & 0xF) == 0 && IsSolverCanceled) return 0UL;
        if (col == n)
            return SymmetryHelper.IsIdentityCanonical(rows, scratch) ? 1UL : 0UL;

        ulong count = 0UL;
        ulong avail = ~(cols | d1 | d2) & fullMask;
        while (avail != 0)
        {
            ulong bit = avail & (ulong)-(long)avail;
            avail ^= bit;
            int r = BitOperations.TrailingZeroCount(bit);

            rows[col] = r;

            // Item 2 gating: short-circuit on reflectionEnabled before the depth-gate test so the
            // ShouldPrunePrefixFull call is skipped entirely when reflection pruning is off (it is
            // the only sound prefix prune here, and the helper returns false immediately in that
            // case anyway). reflectionEnabled is a loop-invariant local, making this branch cheap
            // and the gate self-documenting.
            if (reflectionEnabled && col >= pruneDepthGate &&
                SearchHelpers.ShouldPrunePrefixFull(rows, col, n, reflectionEnabled))
            {
                rows[col] = -1;
                continue;
            }

            count += CountCanonicalDFS(
                col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1,
                n, fullMask, pruneDepthGate, reflectionEnabled, rows, scratch);

            rows[col] = -1;
        }

        return count;
    }
}
