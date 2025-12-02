namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver : ISolver, IDisposable
{
    // Removed regression flag; always use real packed rows.
    private readonly object _sync = new();

    public BitmaskSolver(ISolutionFormatter solutionFormatter,
        int maxDisplayedCount = SimulationSettings.MaxDisplayedCount)
    {
        _formatter = solutionFormatter ?? throw new ArgumentNullException(nameof(solutionFormatter));
        _maxDisplayedCount = maxDisplayedCount;
        _capEnabled = true;
    }

    public BitmaskSolver(ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxDisplayedCount) => _capEnabled = enableCap;

    public BitmaskSolver(int boardSize, SolutionMode solutionMode, DisplayMode displayMode,
        ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxDisplayedCount)
        : this(solutionFormatter, maxSolutionsInOutput)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
        _capEnabled = true;
    }

    // ---------------- Public properties / events ----------------
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec { get; set; }
    public int ProgressValue { get; set; }
    public int BoardSize { get; private set; }
    public SolutionMode SolutionMode { get; private set; }
    public DisplayMode DisplayMode { get; private set; }
    public bool IsSolverCanceled { get; set; }
    public bool EnableEvents { get; set; } = true;
    public ResultStorageMode AllStorageMode { get; set; } = ResultStorageMode.Materialize;
    public ResultStorageMode UniqueStorageMode { get; set; } = ResultStorageMode.Materialize;
    public bool UseCountOnlyUniqueMode { get; set; } = false;
    public bool UseCountOnlyAllMode { get; set; } = false;
    public bool UseParallel { get; set; } = true;
    public int ParallelRootSplitDepth { get; set; } = 1;
    public bool UseAdaptiveDepth { get; set; } = false;
    public bool EnablePrefixMinimalityPruning { get; set; } = false; // Opt #1
    public bool EnableIncrementalCanonicalization { get; set; } = false; // Opt #3 (driver flag)
    public bool EnablePartialReflectionPruning { get; set; } = false; // Opt #14
    public bool EnableMitmAllSplit { get; set; } = false; // Opt #4
    public bool EnableHalfBoardRestriction { get; set; } = false; // new flag (applies to All mode; materialize + count-only)

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext) =>
        Task.Run(() =>
        {
            lock (_sync)
            {
                BoardSize = simContext.BoardSize;
                SolutionMode = simContext.SolutionMode;
                DisplayMode = simContext.DisplayMode;
                return Solve();
            }
        });

    // ---------------- Core Solve ----------------
    public SimulationResults Solve()
    {
        lock (_sync)
        {
            if (BoardSize <= 0)
                throw new InvalidOperationException("BoardSize must be >0.");
            if (BoardSize > BoardSettings.MaxBitmaskBoardSize)
                throw new NotSupportedException($"Bitmask solver supports boards up to {BoardSettings.MaxBitmaskBoardSize}. (Requested: {BoardSize})");
            bool allCountOnly = UseCountOnlyAllMode || AllStorageMode == ResultStorageMode.CountOnly;
            bool uniqueCountOnly = UseCountOnlyUniqueMode || UniqueStorageMode == ResultStorageMode.CountOnly;

            // TEMP: disable events for count-only paths (less overhead in hot loops)
            bool origEnableEvents = EnableEvents;
            if (uniqueCountOnly || allCountOnly)
                EnableEvents = false;

            ResetForSolve();
            Solution.ResetSequence();
            var sw = Stopwatch.StartNew();
            bool usedLookup = false;
            switch (SolutionMode)
            {
                case SolutionMode.All:
                    HandleModeCommon(isUnique: false, countOnly: allCountOnly, ref usedLookup);
                    break;
                case SolutionMode.Unique:
                    HandleModeCommon(isUnique: true, countOnly: uniqueCountOnly, ref usedLookup);
                    break;
                case SolutionMode.Single:
                    SolveSingleMode();
                    break;
                default:
                    throw new NotSupportedException($"Unsupported SolutionMode {SolutionMode}");
            }
            sw.Stop();

            // Restore event flag
            EnableEvents = origEnableEvents;

            var results = BuildResults(sw.Elapsed);
            if (usedLookup)
                return new SimulationResults(results.Solutions, _solutionCount, Math.Round(sw.Elapsed.TotalSeconds, 1));
            return results;
        }
    }

    // Unified handler for Unique/All
    private void HandleModeCommon(bool isUnique, bool countOnly, ref bool usedLookup)
    {
        // Attempt lookup first when board size >= threshold
        if (BoardSize >= _lookupThreshold)
        {
            ulong lookup = isUnique ? ExpectedSolutionCounts.GetUnique(BoardSize) : ExpectedSolutionCounts.GetAll(BoardSize);
            if (lookup > 0)
            {
                _solutionCount = lookup;
                usedLookup = true;
                if (countOnly)
                {
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
                    return; // done
                }
                // Materialize sample solutions using lookup (fast path)
                SampleMaterializeUsingLookup(isUnique);
                return;
            }
        }

        // Below threshold OR lookup unavailable: enumeration path
        if (countOnly)
        {
            if (isUnique)
            {
                // Consolidated: Unique count-only uses adaptive parallel path
                _solutionCount = CountUniqueAdaptive(BoardSize);
            }
            else
            {
                // Consolidated thresholds using SimulationSettings for All mode
                if (BoardSize <= SimulationSettings.ParallelAllAutoEnableThresholdN)
                    _solutionCount = EnumerateAllAndReturnCount();
                else
                    EnumerateAllAdaptive(countOnly: true);
            }
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Materialization path: adaptive enumeration per mode
        if (isUnique)
        {
            // Consolidated: Unique materialize switches to fast mode after cap
            if (DisplayMode == DisplayMode.Visualize)
                EnumerateUniqueVisualizeAdaptive();
            else
                EnumerateUniqueMaterializeAdaptive();
        }
        else
        {
            EnumerateAllAdaptive(countOnly: false);
        }
    }

    // New: Adaptive parallel unique count-only
    private ulong CountUniqueAdaptive(int n)
    {
        // Force pruning + disable incremental canonicalization
        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        SearchOptimizations.Configure(
            EnablePrefixMinimalityPruning,
            EnablePartialReflectionPruning,
            incrementalCanonicalization: false);

        // Ensure ThreadPool saturates cores for parallel runs
        System.Threading.ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

        try
        {
            // Mid-range boards (≥ threshold): use dictionary-free half-board engine
            if (n >= SimulationSettings.UniqueCountOnlyParallelThresholdN && n <= 22)
                return CountUniqueFastHalfBoard(n);

            // Small boards: parallel unified unique engine (cap=0 => count-only)
            if (n < SimulationSettings.UniqueCountOnlyParallelThresholdN)
            {
                ulong total = 0;
                BitmaskParallelEngine.RunUniqueUnified(
                    n,
                    enableEvents: false,
                    cap: 0,
                    onUniqueSolution: _ => { },
                    onCompletedUniqueCount: count => total = count,
                    reportProgress: _ => { },
                    capReached: () => false
                );
                return total;
            }

            // Larger boards: symmetry-pruned counter (memory-efficient)
            return SymmetryPrunedUniqueCounter.Count(n, cap: 0, onMaterialized: null);
        }
        finally
        {
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
            SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        }
    }

    // Level-2: fast unique count-only with half-board restriction, early prefix pruning, pooled scratch
    private ulong CountUniqueFastHalfBoard(int n)
    {
        if (n <= 0) return 0UL;

        // Force pruning locally (do not depend on external flags)
        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        SearchOptimizations.Configure(
            EnablePrefixMinimalityPruning,
            EnablePartialReflectionPruning,
            incrementalCanonicalization: false);

        // Half-board: restrict first column root rows to first half (+ center if odd)
        int firstRowLimitExclusive = (n + 1) / 2;
        ulong fullMask = (n == 64) ? ulong.MaxValue : ((1UL << n) - 1UL);
        int cores = Environment.ProcessorCount;

        // Early prune gate for mid/large N
        int pruneDepthGate = (n >= 18) ? 1 : ((n >= 16) ? 2 : 3);

        // Pool scratch arrays to minimize allocations
        var scratchPool = System.Buffers.ArrayPool<int>.Shared;

        long total = 0L;

        try
        {
            // Partition root rows by ranges to balance across cores
            var ranges = System.Collections.Concurrent.Partitioner.Create(0, firstRowLimitExclusive, Math.Max(1, firstRowLimitExclusive / (cores * 2)));

            Parallel.ForEach(ranges, new ParallelOptions { MaxDegreeOfParallelism = cores }, range =>
            {
                int[] rows = new int[n];
                Array.Fill(rows, -1);

                // Scratch for canonical identity checks
                int[] scratch = scratchPool.Rent(n * 8);
                try
                {
                    ulong localCount = 0UL;

                    for (int rootRow = range.Item1; rootRow < range.Item2; rootRow++)
                    {
                        rows[0] = rootRow;
                        ulong bit0 = 1UL << rootRow;

                        DFS(col: 1, cols: bit0, d1: bit0 << 1, d2: bit0 >> 1);
                    }

                    if (localCount != 0)
                        System.Threading.Interlocked.Add(ref total, (long)localCount);

                    // DFS local function captures rows/scratch/localCount
                    void DFS(int col, ulong cols, ulong d1, ulong d2)
                    {
                        if (IsSolverCanceled) return;
                        if (col == n)
                        {
                            // Leaf: identity canonical representative (no dictionary)
                            if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
                                localCount++;
                            return;
                        }

                        ulong avail = ~(cols | d1 | d2) & fullMask;
                        while (avail != 0 && !IsSolverCanceled)
                        {
                            ulong bit = avail & (ulong)-(long)avail;
                            avail ^= bit;
                            int r = System.Numerics.BitOperations.TrailingZeroCount(bit);

                            rows[col] = r;

                            // Early prefix pruning: reflection + minimality
                            if (col >= pruneDepthGate && (EnablePrefixMinimalityPruning || EnablePartialReflectionPruning))
                            {
                                if (ShouldPruneUniquePrefix(rows, col, n))
                                {
                                    rows[col] = -1;
                                    continue;
                                }
                            }

                            DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                            rows[col] = -1;
                        }
                    }
                }
                finally
                {
                    scratchPool.Return(scratch, clearArray: false);
                }
            });
        }
        finally
        {
            // Restore flags/config (no persistent side-effects)
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
            SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        }

        return (ulong)total;
    }

    // Unique-specific prefix pruning: reflection + minimality checks on the current prefix
    private static bool ShouldPruneUniquePrefix(int[] rows, int depth, int N)
    {
        // Reflection prefix: ensure prefix <= reflected(prefix)
        for (int i = 0; i <= depth; i++)
        {
            int r = rows[i];
            if (r < 0) return false;
            int reflected = N - 1 - r;
            if (r > reflected) return true;
            if (r < reflected) break; // strictly less; reflection constraint satisfied
        }

        // Minimality prefix: transform reversed prefix and compare lexicographically
        for (int i = 0; i <= depth; i++)
        {
            int a = rows[i];
            if (a < 0) return false;
            int b = rows[depth - i];
            if (b < 0) return false;

            int transformed = N - 1 - b;
            if (a > transformed) return true;
            if (a < transformed) break; // strictly less; minimality satisfied
        }

        return false;
    }

    private void EnumerateAllAdaptive(bool countOnly)
    {
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        int N = BoardSize;
        bool halfBoard = N >= 15 && ((N & 1) == 1);
        bool isOdd = (N & 1) == 1; int centerRow = N / 2;
        if (N <= 18)
        {
            int capSmall = countOnly ? 0 : _maxDisplayedCount;
            ulong countNonCenter = 0; ulong countCenter = 0; int materializedSmall = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                N,
                RestrictFirstCol: halfBoard,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: countOnly,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents && !countOnly) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap && !countOnly) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, N)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    int r0 = rows[0];
                    if (halfBoard && isOdd && r0 == centerRow) countCenter++; else countNonCenter++;
                    if (!countOnly && capSmall > 0 && materializedSmall < capSmall)
                    {
                        if (rows.Length <= 25)
                        {
                            UInt128 packed = SymmetryHelper.PackRows(rows);
                            _solutions.Add((packed, rows.Length));
                        }
                        else
                        {
                            var copy = new int[rows.Length];
                            Array.Copy(rows, copy, rows.Length);
                            _largeBoardRawSolutions.Add(copy);
                        }
                        materializedSmall++;
                        if (materializedSmall >= capSmall && _capEnabled)
                            _eventsSuppressedAfterCap = true;
                    }
                    return false;
                }
            ));
            _solutionCount = halfBoard ? (countNonCenter * 2UL + countCenter) : (countNonCenter + countCenter);
            if (EnableEvents && !countOnly) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }
        int cap = countOnly ? 0 : _maxDisplayedCount;
        ulong totalNonCenter = 0; ulong totalCenter = 0; int materialized = 0;
        int cores = Environment.ProcessorCount;
        int maxDepth = 4;
        int depth = 2;
        double branchEstimate = Math.Max(2.0, N * 0.55);
        while (depth < maxDepth && Math.Pow(branchEstimate, depth) < cores * 256) depth++;
        var partialStates = new List<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>();
        ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int firstRowLimit = halfBoard ? (N + 1) / 2 : N;
        for (int rootRow = 0; rootRow < firstRowLimit; rootRow++)
        {
            int[] rows = new int[N]; Array.Fill(rows, -1); rows[0] = rootRow;
            ulong bit = 1UL << rootRow;
            Gen(1, bit, bit << 1, bit >> 1, rows);
        }
        void Gen(int col, ulong cols, ulong d1, ulong d2, int[] rows)
        {
            if (col == N || col == depth)
            {
                var snap = new int[N]; Array.Copy(rows, snap, N);
                partialStates.Add((col, snap, cols, d1, d2));
                return;
            }
            ulong avail = ~(cols | d1 | d2) & fullMask;
            for (ulong a = avail; a != 0; a &= (a - 1))
            {
                ulong bitLocal = a & (ulong)-(long)a;
                int row = BitOperations.TrailingZeroCount(bitLocal);
                rows[col] = row;
                ulong nextCols = cols | bitLocal;
                ulong nextD1 = (d1 | bitLocal) << 1;
                ulong nextD2 = (d2 | bitLocal) >> 1;
                Gen(col + 1, nextCols, nextD1, nextD2, rows);
                rows[col] = -1;
            }
        }
        if (partialStates.Count == 0) { _solutionCount = 0; return; }

        // Work-stealing queue with batched tasks
        var queue = new ConcurrentQueue<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>();
        foreach (var ps in partialStates) queue.Enqueue(ps);
        int batchSize = Math.Max(8, partialStates.Count / (cores * 16));
        var tasks = new List<Task>(cores);
        var poolCols = System.Buffers.ArrayPool<ulong>.Shared;
        var poolD1 = System.Buffers.ArrayPool<ulong>.Shared;
        var poolD2 = System.Buffers.ArrayPool<ulong>.Shared;
        var poolAvail = System.Buffers.ArrayPool<ulong>.Shared;
        object mergeLock = new();
        for (int t = 0; t < cores; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong threadNonCenter = 0; ulong threadCenter = 0; int threadMat = 0;
                var stackCols = poolCols.Rent(N);
                var stackD1 = poolD1.Rent(N);
                var stackD2 = poolD2.Rent(N);
                var stackAvail = poolAvail.Rent(N);
                try
                {
                    var batch = new List<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>(batchSize);
                    while (!queue.IsEmpty && !IsSolverCanceled)
                    {
                        batch.Clear();
                        for (int i = 0; i < batchSize && queue.TryDequeue(out var item); i++) batch.Add(item);
                        if (batch.Count == 0) break;
                        foreach (var item in batch)
                        {
                            var startCol = item.col;
                            var rows = item.rows;
                            ulong colsLocal = item.cols;
                            ulong d1Local = item.d1;
                            ulong d2Local = item.d2;
                            int col = startCol;
                            ulong avail = ~(colsLocal | d1Local | d2Local) & fullMask;
                            while (true)
                            {
                                if (IsSolverCanceled) break;
                                if (col == N)
                                {
                                    int r0 = rows[0];
                                    if (halfBoard && isOdd && r0 == centerRow) threadCenter++; else threadNonCenter++;
                                    if (!countOnly && cap > 0 && threadMat < cap)
                                    {
                                        int current = System.Threading.Interlocked.Increment(ref materialized);
                                        if (current <= cap)
                                        {
                                            lock (mergeLock)
                                            {
                                                if (rows.Length <= 25)
                                                {
                                                    UInt128 packed = SymmetryHelper.PackRows(rows);
                                                    _solutions.Add((packed, rows.Length));
                                                }
                                                else
                                                {
                                                    var copy = new int[rows.Length];
                                                    Array.Copy(rows, copy, rows.Length);
                                                    _largeBoardRawSolutions.Add(copy);
                                                }
                                                if (EnableEvents && !_eventsSuppressedAfterCap)
                                                    SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                                                if (current == cap) _eventsSuppressedAfterCap = true;
                                            }
                                        }
                                        threadMat = current;
                                    }
                                    col--; if (col < startCol) break;
                                    avail = stackAvail[col]; colsLocal = stackCols[col]; d1Local = stackD1[col]; d2Local = stackD2[col]; rows[col] = -1;
                                    continue;
                                }
                                if (avail == 0UL)
                                {
                                    col--; if (col < startCol) break;
                                    avail = stackAvail[col]; colsLocal = stackCols[col]; d1Local = stackD1[col]; d2Local = stackD2[col]; rows[col] = -1;
                                    continue;
                                }
                                ulong bitLocal = avail & (ulong)-(long)avail; avail ^= bitLocal;
                                int row = BitOperations.TrailingZeroCount(bitLocal);
                                rows[col] = row;
                                stackCols[col] = colsLocal; stackD1[col] = d1Local; stackD2[col] = d2Local; stackAvail[col] = avail;
                                colsLocal |= bitLocal; d1Local = (d1Local | bitLocal) << 1; d2Local = (d2Local | bitLocal) >> 1;
                                col++; if (col == N) continue;
                                avail = ~(colsLocal | d1Local | d2Local) & fullMask;
                            }
                        }
                    }
                }
                finally
                {
                    poolCols.Return(stackCols, clearArray: false);
                    poolD1.Return(stackD1, clearArray: false);
                    poolD2.Return(stackD2, clearArray: false);
                    poolAvail.Return(stackAvail, clearArray: false);
                }
                lock (mergeLock)
                {
                    totalNonCenter += threadNonCenter;
                    totalCenter += threadCenter;
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        ulong combined = halfBoard ? (totalNonCenter * 2UL + totalCenter) : (totalNonCenter + totalCenter);
        _solutionCount = combined;
        if (EnableEvents && !countOnly) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void EnumerateUniqueMaterializeAdaptive()
    {
        // Consolidated: materialize up to cap, then switch to fast count-only (no more materialization)
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, EnableIncrementalCanonicalization);
        int cap = _maxDisplayedCount;
        if (cap <= 0)
        {
            _solutionCount = CountUniqueCanonicalOrPruned(BoardSize, cap: 0, null);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        int materialized = 0;
        ulong totalSoFar = 0;

        // Materialize up to cap
        totalSoFar = CountUniqueCanonicalOrPruned(BoardSize, cap, rows =>
        {
            if (materialized >= cap) return;
            if (rows.Length <= 25)
            {
                UInt128 packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                _solutions.Add((packed, rows.Length));
            }
            else
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
            }
            materialized++;
            if (EnableEvents && !_eventsSuppressedAfterCap)
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
            if (materialized >= cap)
                _eventsSuppressedAfterCap = true;
        });

        // If we reached cap, switch to fast count-only for the remainder
        if (materialized >= cap)
        {
            // Choose fast path based on thresholds (same constants as All mode)
            ulong remainderCount;
            if (BoardSize <= SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN)
            {
                // Small boards: canonical unique (no parallel key store overhead)
                remainderCount = CountUniqueCanonicalOrPruned(BoardSize, cap: 0, onMaterialized: null);
            }
            else
            {
                // Larger boards: parallel unified unique count-only
                remainderCount = CountUniqueAdaptive(BoardSize);
            }
            _solutionCount = remainderCount;
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // If cap not reached by enumeration, we already have total from canonical counting
        _solutionCount = totalSoFar;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private ulong CountUniqueCanonicalOrPruned(int boardSize, int cap, Action<int[]>? onMaterialized)
    {
        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            return SymmetryPrunedUniqueCounter.Count(boardSize, cap, onMaterialized);
        }
        ulong total = 0;
        int emitted = 0;
        CanonicalUniqueSearchEngine.CountUnique(boardSize, rows =>
        {
            if (onMaterialized == null) return;
            if (cap > 0 && emitted >= cap) return;
            onMaterialized(rows);
            emitted++;
        });
        total = CanonicalUniqueSearchEngine.CountUnique(boardSize);
        return total;
    }

    // Private fields
    private readonly ISolutionFormatter _formatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly List<int[]> _largeBoardRawSolutions = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled;
    private readonly int _maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private bool _disposed;
    private const int _lookupThreshold = 20;
    private const int _largeBoardConstructiveThreshold = 20;
    private int[]? _scratchBuffer;

    private void ResetForSolve()
    {
        _solutions.Clear();
        _largeBoardRawSolutions.Clear();
        _solutionCount = 0;
        IsSolverCanceled = false;
        _eventsSuppressedAfterCap = false;
        _scratchBuffer = (_scratchBuffer == null || _scratchBuffer.Length < BoardSize * 8)
            ? new int[BoardSize * 8]
            : _scratchBuffer;
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var cap = (_capEnabled ? _maxDisplayedCount : 0);
        var resultSolutions = new List<Solution>(_solutions.Count + _largeBoardRawSolutions.Count);
        int idx = 1;

        foreach (var tup in (cap > 0 && _solutions.Count > cap ? _solutions.Take(cap) : _solutions))
        {
            var packed = tup.packed;
            var boardSize = tup.boardSize;
            if (boardSize <= 0) continue;
            if (boardSize <= 25)
            {
                resultSolutions.Add(new Solution(packed, boardSize, _formatter, idx));
                idx++;
            }
        }

        foreach (var raw in _largeBoardRawSolutions)
        {
            if (cap > 0 && idx > cap) break;
            resultSolutions.Add(new Solution(raw, _formatter, idx));
            idx++;
        }

        _largeBoardRawSolutions.Clear();
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private void SampleMaterializeUsingLookup(bool isUnique)
    {
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, EnableIncrementalCanonicalization);
        int cap = _maxDisplayedCount;
        if (cap <= 0) return;

        if (BoardSize >= _largeBoardConstructiveThreshold)
        {
            ConstructiveSampleSolutions(isUnique, cap);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        if (isUnique)
        {
            var seen = new HashSet<UInt128>();
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    UInt128 packed = 0;
                    if (rows.Length <= 25)
                        packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                    if (seen.Add(packed))
                    {
                        AddSample(rows);
                        if (seen.Count >= cap)
                        {
                            _eventsSuppressedAfterCap = true;
                            return true;
                        }
                    }
                    return false;
                }
            ));
        }
        else
        {
            int materialized = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (materialized < cap)
                    {
                        AddSample(rows);
                        materialized++;
                        if (materialized >= cap)
                        {
                            _eventsSuppressedAfterCap = true;
                            return true;
                        }
                    }
                    return false;
                }
            ));
        }

        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));

        void AddSample(int[] rows)
        {
            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                _solutions.Add((packed, rows.Length));
            }
            else
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
            }
            if (EnableEvents && !_eventsSuppressedAfterCap)
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
        }
    }

    private bool ValidateRows(int[] rows)
    {
        bool ok = rows.Length == BoardSize;
        Debug.Assert(ok, $"[BitmaskSolver] Invalid solution rows length={rows.Length}, BoardSize={BoardSize}");
        return ok;
    }

    // IDisposable implementation (needed by interface)
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _solutions.Clear();
            _largeBoardRawSolutions.Clear();
            QueenPlaced = null;
            SolutionFound = null;
            ProgressValueChanged = null;
        }
        _disposed = true;
    }

    // Construct sample solutions for large boards (fast path when using lookup/materialize)
    private void ConstructiveSampleSolutions(bool isUnique, int cap)
    {
        if (cap <= 0) return;

        var baseRows = GenerateConstructiveSolution(BoardSize);
        if (!ValidateRows(baseRows)) return;

        AddMaterialized(baseRows);
        if (cap == 1) return;

        int remaining = cap - 1;
        var variants = GenerateSymmetryVariants(baseRows, remaining);
        foreach (var v in variants) AddMaterialized(v);

        void AddMaterialized(int[] rows)
        {
            if (_solutions.Count + _largeBoardRawSolutions.Count >= cap) return;

            if (isUnique)
            {
                // For unique mode in constructive sampling, keep raw rows (canonicalization happens elsewhere)
                var copyU = new int[rows.Length];
                Array.Copy(rows, copyU, rows.Length);
                _largeBoardRawSolutions.Add(copyU);
                return;
            }

            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                _solutions.Add((packed, rows.Length));
            }
            else
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
            }
        }
    }

    // Standard constructive N-Queens sequence generator (O(n))
    private static int[] GenerateConstructiveSolution(int n)
    {
        var seq = new List<int>(n);

        if (n % 6 != 2 && n % 6 != 3)
        {
            for (int i = 2; i <= n; i += 2) seq.Add(i);
            for (int i = 1; i <= n; i += 2) seq.Add(i);
        }
        else if (n % 6 == 2)
        {
            for (int i = 2; i <= n; i += 2) seq.Add(i);
            for (int i = 1; i <= n; i += 2) seq.Add(i);
            if (seq.Count >= 4) (seq[0], seq[1]) = (seq[1], seq[0]);
        }
        else
        {
            for (int i = 2; i <= n - 1; i += 2) seq.Add(i);
            for (int i = 1; i <= n - 2; i += 2) seq.Add(i);
            seq.Add(n);
        }

        var rows = new int[n];
        for (int col = 0; col < n; col++)
            rows[col] = seq[col] - 1; // zero-based rows

        return rows;
    }

    // Generate up to 7 symmetry variants (rotations/reflections) of a solution
    private static IEnumerable<int[]> GenerateSymmetryVariants(int[] rows, int maxVariants)
    {
        var list = new List<int[]>(Math.Min(maxVariants, 7));
        void AddVariant(int[] r) { if (list.Count < maxVariants) list.Add(r); }

        int n = rows.Length;

        int[] Rotate90(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
            {
                int oldRow = src[c];
                int newCol = oldRow;
                int newRow = n - 1 - c;
                r[newCol] = newRow;
            }
            return r;
        }

        int[] ReflectVertical(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
                r[n - 1 - c] = src[c];
            return r;
        }

        int[] ReflectHorizontal(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
                r[c] = n - 1 - src[c];
            return r;
        }

        var r90 = Rotate90(rows); AddVariant(r90);
        var r180 = Rotate90(r90); AddVariant(r180);
        var r270 = Rotate90(r180); AddVariant(r270);
        var vref = ReflectVertical(rows); AddVariant(vref);
        var href = ReflectHorizontal(rows); AddVariant(href);
        var diag = ReflectVertical(r90); AddVariant(diag);

        return list;
    }

    private ulong EnumerateAllAndReturnCount()
    {
        EnumerateAllAdaptive(countOnly: true);
        return _solutionCount;
    }
}
