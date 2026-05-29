namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver(ISolutionFormatter solutionFormatter,
    int maxDisplayedCount = SimulationSettings.MaxDisplayedCount) : ISolver, IDisposable
{

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

    private int _delayInMillisec; // enforce min delay via setter below

    // ---------------- Public properties / events ----------------
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec
    {
        get => _delayInMillisec;
        set => _delayInMillisec = value <= 0 ? 0 : Math.Max(NQueen.Domain.Settings.SimulationSettings.MinDelayInMilliseconds, value);
    }

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
        // Attempt lookup first when board size >= global threshold
        if (BoardSize >= NQueen.Domain.Settings.SimulationSettings.LookupThresholdN)
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
                _solutionCount = CountUniqueAdaptive(BoardSize);
            }
            else
            {
                // Auto-tune All count-only for performance
                if (BoardSize >= SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN)
                {
                    EnablePrefixMinimalityPruning = false;
                    EnablePartialReflectionPruning = false;
                    UseAdaptiveDepth = true;
                    ParallelRootSplitDepth = 3;
                }

                EnumerateAllAdaptive(countOnly: true);
            }
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Materialization path: adaptive enumeration per mode
        if (isUnique)
        {
            if (DisplayMode == DisplayMode.Visualize)
                EnumerateUniqueVisualizeAdaptive();
            else
                ExecuteUniqueModeUnified();
        }
        else
        {
            EnumerateAllAdaptive(countOnly: false);
        }
    }

    private ulong CountUniqueAdaptive(int n)
    {
        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        SearchOptimizations.Configure(
            EnablePrefixMinimalityPruning,
            EnablePartialReflectionPruning,
            incrementalCanonicalization: false);

        ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

        try
        {
            // CountUniqueAdaptive is only called for N <= LookupThresholdN-1 (20).
            // The two branches below are exhaustive for that range; no fallthrough exists.
            if (n >= SimulationSettings.UniqueCountOnlyParallelThresholdN)
            {
                // N= 16 .. 20: half-board parallel DFS — the same algorithm used by CountOnly.
                // The former n<=22 upper cap is removed; the lookup table makes N>=21 unreachable here.
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
            SearchOptimizations.Configure(
                EnablePrefixMinimalityPruning,
                EnablePartialReflectionPruning,
                incrementalCanonicalization: false);
        }
    }

    // Fast unique count-only half-board path
    private ulong CountUniqueFastHalfBoard(int n)
    {
        if (n <= 0) return 0UL;

        bool origPrefix = EnablePrefixMinimalityPruning;
        bool origReflection = EnablePartialReflectionPruning;
        EnablePrefixMinimalityPruning = true;
        EnablePartialReflectionPruning = true;

        SearchOptimizations.Configure(
            EnablePrefixMinimalityPruning,
            EnablePartialReflectionPruning,
            incrementalCanonicalization: false);

        int firstRowLimitExclusive = (n + 1) / 2;
        ulong fullMask = (n == 64) ? ulong.MaxValue : ((1UL << n) - 1UL);
        int cores = Environment.ProcessorCount;

        int pruneDepthGate = int.MaxValue;
        if (EnablePrefixMinimalityPruning || EnablePartialReflectionPruning)
        {
            // Push earliest pruning for N>=20
            if (n >= 20) pruneDepthGate = 1;
            else if (n >= SimulationSettings.PrefixPruneEarlyThresholdN) pruneDepthGate = 0;
            else if (n >= 16) pruneDepthGate = 2;
            else if (n >= SimulationSettings.LargeBoardSymmetryPruningThreshold) pruneDepthGate = 3;
        }

        var scratchPool = ArrayPool<int>.Shared;
        long total = 0L;

        try
        {
            ThreadPool.SetMinThreads(cores, cores);

            // Larger chunks reduce scheduler overhead on very large N
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

                    for (int rootRow = range.Item1; rootRow < range.Item2; rootRow++)
                    {
                        rows[0] = rootRow;
                        ulong bit0 = 1UL << rootRow;

                        bool reflectionEqual = true;
                        bool minimalityEqual = true;
                        DFS(col: 1, cols: bit0, d1: bit0 << 1, d2: bit0 >> 1,
                            reflectionEnabled: EnablePartialReflectionPruning, minimalityEnabled:
                            EnablePrefixMinimalityPruning, pruneDepthGate, ref reflectionEqual, ref minimalityEqual);
                    }

                    if (localCount != 0)
                        Interlocked.Add(ref total, (long)localCount);

                    void DFS(int col, ulong cols, ulong d1, ulong d2, bool reflectionEnabled,
                        bool minimalityEnabled, int pruneGate, ref bool reflectionEqual, ref bool minimalityEqual)
                    {
                        // Check cancellation less frequently (every 16 depth levels) to reduce overhead
                        if ((col & 0xF) == 0 && IsSolverCanceled) return;
                        if (col == n)
                        {
                            if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
                                localCount++;
                            return;
                        }

                        ulong avail = ~(cols | d1 | d2) & fullMask;
                        // Precompute whether we need symmetry checks to avoid repeated flag evaluations
                        bool needSymmetryCheck = col >= pruneGate &&
                            ((reflectionEnabled && reflectionEqual) || (minimalityEnabled && minimalityEqual));

                        while (avail != 0)
                        {
                            ulong bit = avail & (ulong)-(long)avail;
                            avail ^= bit;
                            // Prefer JIT intrinsic for trailing zero count in the hot loop
                            int r = BitOperations.TrailingZeroCount(bit);

                            rows[col] = r;

                            // If early-equality already broke, skip symmetry checks quickly
                            if (needSymmetryCheck)
                            {
                                bool savedReflectionEqual = reflectionEqual;
                                bool savedMinimalityEqual = minimalityEqual;

                                if (SearchOptimizations.ShouldPrunePrefixIncremental(rows, col, n, reflectionEnabled, minimalityEnabled,
                                    ref reflectionEqual, ref minimalityEqual))
                                {
                                    reflectionEqual = savedReflectionEqual;
                                    minimalityEqual = savedMinimalityEqual;
                                    rows[col] = -1;
                                    continue;
                                }

                                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1, reflectionEnabled, minimalityEnabled, pruneGate, ref reflectionEqual, ref minimalityEqual);
                                reflectionEqual = savedReflectionEqual;
                                minimalityEqual = savedMinimalityEqual;
                            }
                            else
                            {
                                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1, reflectionEnabled, minimalityEnabled, pruneGate, ref reflectionEqual, ref minimalityEqual);
                            }

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
            EnablePrefixMinimalityPruning = origPrefix;
            EnablePartialReflectionPruning = origReflection;
            SearchOptimizations.Configure(
                EnablePrefixMinimalityPruning,
                EnablePartialReflectionPruning,
                incrementalCanonicalization: false);
        }

        return (ulong)total;
    }

    // ------------ private fields and helpers ------------
    private readonly ISolutionFormatter _formatter = solutionFormatter ?? throw new ArgumentNullException(nameof(solutionFormatter));
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly List<int[]> _largeBoardRawSolutions = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private readonly int _maxDisplayedCount = maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private bool _disposed;
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

        if (BoardSize >= SimulationSettings.ConstructiveSampleThresholdN)
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
                p =>
                {
                    if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken));
                },
                m =>
                {
                    if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize));
                },
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
            int materializedSamples = 0;
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
                p =>
                {
                    if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken));
                },
                m =>
                {
                    if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(
                    this, new QueenPlacedEventArgs(m, BoardSize));
                },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (materializedSamples < cap)
                    {
                        AddSample(rows);
                        materializedSamples++;
                        if (materializedSamples >= cap)
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

    // Constructive helpers (kept here so SampleMaterializeUsingLookup compiles)
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
                var copyU = new int[rows.Length];
                Array.Copy(rows, copyU, rows.Length);
                _largeBoardRawSolutions.Add(copyU);
                return;
            }

            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer ?? new int[rows.Length * 8], out _);
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
            rows[col] = seq[col] - 1;
        return rows;
    }

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

    private readonly object _sync = new();
}
