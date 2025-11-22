namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver : ISolver, IDisposable
{
    private readonly object _sync = new(); // synchronization root

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
                    SolveSingleMode(); // defined in BitmaskSolver.Single.cs partial
                    break;
                default:
                    throw new NotSupportedException($"Unsupported SolutionMode {SolutionMode}");
            }
            sw.Stop();
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
                _solutionCount = CountUniqueCanonicalOrPruned(BoardSize, cap: 0, null);
            }
            else
            {
                // For moderate boards rely on exact path; for larger boards use adaptive parallel enumeration (count-only).
                if (BoardSize <= 18)
                    _solutionCount = CountAllExact();
                else
                {
                    // Run adaptive enumeration without materialization.
                    EnumerateAllAdaptive(countOnly: true);
                }
            }
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Materialization path: adaptive enumeration per mode
        if (isUnique)
        {
            EnumerateUniqueMaterializeAdaptive();
        }
        else
        {
            EnumerateAllAdaptive(countOnly: false);
        }
    }

    private ulong CountAllExact()
    {
        // Disable incremental canonicalization for All mode enumeration
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        ulong count = 0UL;
        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
            rows => { if (!ValidateRows(rows)) return false; count++; return false; }
        ));
        return count;
    }

    private void EnumerateUniqueMaterializeAdaptive()
    {
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, EnableIncrementalCanonicalization);
        int cap = _maxDisplayedCount;
        if (cap <= 0)
        {
            _solutionCount = CountUniqueCanonicalOrPruned(BoardSize, cap: 0, null);
            return;
        }
        int materialized = 0;
        ulong total = CountUniqueCanonicalOrPruned(BoardSize, cap, rows =>
        {
            if (materialized >= cap) return;
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
            materialized++;
            if (EnableEvents && !_eventsSuppressedAfterCap)
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
            if (materialized >= cap)
                _eventsSuppressedAfterCap = true;
        });
        _solutionCount = total;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
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
                    if (rows.Length <= 25) packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
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
        for (int col = 0; col < n; col++) rows[col] = seq[col] - 1;
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
            for (int c = 0; c < n; c++) { int oldRow = src[c]; int newCol = oldRow; int newRow = n - 1 - c; r[newCol] = newRow; }
            return r;
        }
        int[] ReflectVertical(int[] src)
        { var r = new int[n]; for (int c = 0; c < n; c++) r[n - 1 - c] = src[c]; return r; }
        int[] ReflectHorizontal(int[] src)
        { var r = new int[n]; for (int c = 0; c < n; c++) r[c] = n - 1 - src[c]; return r; }
        var r90 = Rotate90(rows); AddVariant(r90);
        var r180 = Rotate90(r90); AddVariant(r180);
        var r270 = Rotate90(r180); AddVariant(r270);
        var vref = ReflectVertical(rows); AddVariant(vref);
        var href = ReflectHorizontal(rows); AddVariant(href);
        var diag = ReflectVertical(r90); AddVariant(diag);
        return list;
    }

    private bool ValidateRows(int[] rows)
    {
        bool ok = rows.Length == BoardSize;
        Debug.Assert(ok, $"[BitmaskSolver] Invalid solution rows length={rows.Length}, BoardSize={BoardSize}");
        return ok;
    }

    private void ResetForSolve()
    {
        _solutions.Clear();
        _largeBoardRawSolutions.Clear();
        _solutionCount = 0;
        IsSolverCanceled = false;
        _eventsSuppressedAfterCap = false;
        _scratchBuffer = (_scratchBuffer == null || _scratchBuffer.Length < BoardSize * 8) ? new int[BoardSize * 8] : _scratchBuffer;
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
            if (boardSize <= 25) { resultSolutions.Add(new Solution(packed, boardSize, _formatter, idx)); idx++; }
        }
        foreach (var raw in _largeBoardRawSolutions)
        {
            if (cap > 0 && idx > cap) break;
            resultSolutions.Add(new Solution(raw, _formatter, idx)); idx++;
        }
        _largeBoardRawSolutions.Clear();
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
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

    // --- All-mode adaptive enumeration (modified to disable incremental canonicalization locally) ---
    private void EnumerateAllAdaptive(bool countOnly)
    {
        // Disable incremental canonicalization for All mode enumeration
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        int N = BoardSize;
        if (N <= 18)
        {
            int capSmall = countOnly ? 0 : _maxDisplayedCount;
            ulong countSmall = 0;
            int materializedSmall = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                N,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, N)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    countSmall++;
                    if (capSmall > 0 && materializedSmall < capSmall)
                    {
                        _solutions.Add((0, rows.Length));
                        materializedSmall++;
                        if (materializedSmall >= capSmall && _capEnabled)
                            _eventsSuppressedAfterCap = true;
                    }
                    return false;
                }
            ));
            _solutionCount = countSmall;
            if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }
        int cap = countOnly ? 0 : _maxDisplayedCount;
        ulong totalCount = 0;
        int materialized = 0;
        int cores = Environment.ProcessorCount;
        int targetJobs = cores * 128;
        int maxDepth = 4;
        int depth = 2;
        double branchEstimate = Math.Max(2.0, N * 0.55);
        while (depth < maxDepth && Math.Pow(branchEstimate, depth) < targetJobs) depth++;
        var partialStates = new List<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>();
        bool abortGen = false;
        int maxStates = targetJobs * 2;
        ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        for (int rootRow = 0; rootRow < N && !abortGen; rootRow++)
        {
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = rootRow;
            ulong bit = 1UL << rootRow;
            Gen(1, bit, bit << 1, bit >> 1, rows);
        }
        void Gen(int col, ulong cols, ulong d1, ulong d2, int[] rows)
        {
            if (abortGen) return;
            if (col == N || col == depth)
            {
                var snap = new int[N];
                Array.Copy(rows, snap, N);
                partialStates.Add((col, snap, cols, d1, d2));
                if (partialStates.Count >= maxStates) abortGen = true;
                return;
            }
            ulong avail = ~(cols | d1 | d2) & fullMask;
            while (avail != 0 && !abortGen)
            {
                ulong bitLocal = avail & (ulong)-(long)avail;
                avail ^= bitLocal;
                int row = BitOperations.TrailingZeroCount(bitLocal);
                rows[col] = row;
                Gen(col + 1, cols | bitLocal, (d1 | bitLocal) << 1, (d2 | bitLocal) >> 1, rows);
                rows[col] = -1;
            }
        }
        int totalJobs = partialStates.Count;
        if (totalJobs == 0) { _solutionCount = 0; return; }
        void DFSWrapper(int startCol, int[] rows, ulong cols, ulong d1, ulong d2)
        {
            void DFS(int col, ulong lc, ulong ld1, ulong ld2)
            {
                if (IsSolverCanceled) return;
                if (col == N)
                {
                    Interlocked.Increment(ref totalCount);
                    if (cap > 0 && materialized < cap)
                    {
                        int current = Interlocked.Increment(ref materialized);
                        if (current <= cap)
                        {
                            _solutions.Add((0, rows.Length));
                            if (EnableEvents && !_eventsSuppressedAfterCap)
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                            if (current == cap) _eventsSuppressedAfterCap = true;
                        }
                    }
                    return;
                }
                ulong avail = ~(lc | ld1 | ld2) & fullMask;
                while (avail != 0 && !IsSolverCanceled)
                {
                    ulong bitLocal = avail & (ulong)-(long)avail;
                    avail ^= bitLocal;
                    int row = BitOperations.TrailingZeroCount(bitLocal);
                    rows[col] = row;
                    DFS(col + 1, lc | bitLocal, (ld1 | bitLocal) << 1, (ld2 | bitLocal) >> 1);
                    rows[col] = -1;
                }
            }
            DFS(startCol, cols, d1, d2);
        }
        if (UseParallel)
        {
            Parallel.ForEach(partialStates, new ParallelOptions { MaxDegreeOfParallelism = cores }, state =>
            {
                var (startCol, rows, cols, d1, d2) = state;
                DFSWrapper(startCol, rows, cols, d1, d2);
            });
        }
        else
        {
            foreach (var state in partialStates)
            {
                var (startCol, rows, cols, d1, d2) = state;
                DFSWrapper(startCol, rows, cols, d1, d2);
            }
        }
        _solutionCount = totalCount;
        if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void MitmEnumerateAll(bool countOnly)
    {
        // Disable incremental canonicalization for All mode MITM enumeration
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, incrementalCanonicalization: false);
        int N = BoardSize;
        int splitDepth = Math.Min(3, N / 2);
        int cap = countOnly ? 0 : _maxDisplayedCount;
        var partials = new List<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>();
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        void Gen(int col, ulong cols, ulong d1, ulong d2, int[] rows)
        {
            if (col == splitDepth)
            {
                var snap = new int[N]; Array.Copy(rows, snap, N);
                partials.Add((col, snap, cols, d1, d2));
                return;
            }
            ulong avail = ~(cols | d1 | d2) & mask;
            while (avail != 0)
            {
                ulong bitLocal = avail & (ulong)-(long)avail; avail ^= bitLocal;
                int r = BitOperations.TrailingZeroCount(bitLocal);
                rows[col] = r;
                Gen(col + 1, cols | bitLocal, (d1 | bitLocal) << 1, (d2 | bitLocal) >> 1, rows);
                rows[col] = -1;
            }
        }
        var seed = new int[N]; Array.Fill(seed, -1);
        Gen(0, 0UL, 0UL, 0UL, seed);
        ulong total = 0; int materialized = 0;
        object sync = new();
        Parallel.ForEach(partials, part =>
        {
            var (col, rows, cols, d1, d2) = part;
            void DFS(int c, ulong lc, ulong ld1, ulong ld2)
            {
                if (IsSolverCanceled) return;
                if (c == N)
                {
                    Interlocked.Increment(ref total);
                    if (cap > 0 && materialized < cap)
                    {
                        int current = Interlocked.Increment(ref materialized);
                        if (current <= cap)
                        {
                            lock (sync) { _solutions.Add((0, N)); }
                        }
                    }
                    return;
                }
                ulong avail = ~(lc | ld1 | ld2) & mask;
                while (avail != 0 && !IsSolverCanceled)
                {
                    ulong bitLocal = avail & (ulong)-(long)avail; avail ^= bitLocal;
                    int r = BitOperations.TrailingZeroCount(bitLocal);
                    rows[c] = r;
                    if (EnablePrefixMinimalityPruning || EnablePartialReflectionPruning)
                    {
                        if (ShouldPrunePrefix(rows, c)) { rows[c] = -1; continue; }
                    }
                    DFS(c + 1, lc | bitLocal, (ld1 | bitLocal) << 1, (ld2 | bitLocal) >> 1);
                    rows[c] = -1;
                }
            }
            DFS(col, cols, d1, d2);
        });
        _solutionCount = total;
        if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private bool ShouldPrunePrefix(int[] rows, int depth)
    {
        int[] prefix = new int[depth + 1];
        for (int i = 0; i <= depth; i++) prefix[i] = rows[i];
        if (EnablePartialReflectionPruning)
        {
            for (int i = 0; i <= depth; i++)
            {
                int reflected = BoardSize - 1 - prefix[i];
                if (prefix[i] > reflected) return true;
                if (prefix[i] < reflected) break;
            }
        }
        if (!EnablePrefixMinimalityPruning) return false;
        for (int i = 0; i <= depth; i++)
        {
            int transformed = BoardSize - 1 - prefix[depth - i];
            if (prefix[i] > transformed) return true;
            if (prefix[i] < transformed) break;
        }
        return false;
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
}
