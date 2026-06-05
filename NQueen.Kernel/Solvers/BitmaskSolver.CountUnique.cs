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
            int chunk = Math.Max(1, firstRowLimitExclusive / (cores * 2));
            var ranges = Partitioner.Create(0, firstRowLimitExclusive, chunk);
            var po = new ParallelOptions { MaxDegreeOfParallelism = cores };

            Parallel.ForEach(ranges, po, range =>
            {
                int[] rows = new int[n];
                Array.Fill(rows, -1);

                int[] scratch = scratchPool.Rent(n * 8);
                try
                {
                    ulong localCount = 0UL;

                    // Stateless reflection-only prefix pruning. ShouldPrunePrefixFull re-scans
                    // columns 0..col each call, so gating it at col >= pruneDepthGate is safe.
                    // Only horizontal reflection is a sound forward-prefix prune for canonical
                    // counting; IsIdentityCanonical at the leaf is the final arbiter across all
                    // eight symmetries, so the half-board root partition and the delayed gate
                    // never drop a valid canonical solution.
                    void DFS(int col, ulong cols, ulong d1, ulong d2)
                    {
                        if ((col & 0xF) == 0 && IsSolverCanceled) return;
                        if (col == n)
                        {
                            if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
                                localCount++;
                            return;
                        }

                        ulong avail = ~(cols | d1 | d2) & fullMask;
                        while (avail != 0)
                        {
                            ulong bit = avail & (ulong)-(long)avail;
                            avail ^= bit;
                            int r = BitOperations.TrailingZeroCount(bit);

                            rows[col] = r;

                            if (col >= pruneDepthGate &&
                                SearchHelpers.ShouldPrunePrefixFull(rows, col, n, reflectionEnabled))
                            {
                                rows[col] = -1;
                                continue;
                            }

                            DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);

                            rows[col] = -1;
                        }
                    }

                    for (int rootRow = range.Item1; rootRow < range.Item2; rootRow++)
                    {
                        rows[0] = rootRow;
                        ulong bit0 = 1UL << rootRow;
                        DFS(1, bit0, bit0 << 1, bit0 >> 1);
                    }

                    if (localCount != 0)
                        Interlocked.Add(ref total, (long)localCount);
                }
                finally
                {
                    scratchPool.Return(scratch, clearArray: false);
                }
            });
        }
        finally
        {
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
        }

        return (ulong)total;
    }
}
