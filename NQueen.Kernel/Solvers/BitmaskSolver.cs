using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NQueen.Domain.Enums;
using NQueen.Domain.Interfaces;
using NQueen.Domain.Models;
using NQueen.Domain.Settings;
using NQueen.Domain.Utils;

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
            Solution.ResetSequence(); // ensure numbering starts at 1 for each solve
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
                _solutionCount = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this);
            }
            else
            {
                _solutionCount = CountAllExact();
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
        ))
        ;
        return count;
    }

    private void EnumerateUniqueMaterializeAdaptive()
    {
        int cap = _maxDisplayedCount;
        if (cap <= 0) { _solutionCount = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this); return; }
        int materialized = 0;
        // Callback collects canonical representatives until cap.
        ulong total = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this, cap: cap, onMaterialized: rows =>
        {
            if (materialized >= cap) return; // safety
            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
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
            if (materialized >= cap) _eventsSuppressedAfterCap = true;
        });
        _solutionCount = total;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Enumerate Unique or All solutions only until cap for sample display; total count from lookup.
    private void SampleMaterializeUsingLookup(bool isUnique)
    {
        int cap = _maxDisplayedCount; // always respect display cap for sample mode
        if (cap <= 0) return;

        // Large-board constructive sampling
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
                    if (rows.Length <= 25) packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
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
                var packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
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

    // Constructive generator using known pattern (even rows then odd rows with adjustments) + symmetry transforms
    private void ConstructiveSampleSolutions(bool isUnique, int cap)
    {
        // Use constant from settings (avoid implicit literal variant counts)
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
                // keep raw variant to show diversity
                var copyU = new int[rows.Length];
                Array.Copy(rows, copyU, rows.Length);
                _largeBoardRawSolutions.Add(copyU);
                return;
            }
            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
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
        // Classic constructive algorithm adapted to produce a valid solution in O(n)
        // Sequence generation in 1-based then convert to 0-based rows.
        var seq = new List<int>(n);
        if (n % 6 != 2 && n % 6 != 3)
        {
            // even numbers then odd numbers
            for (int i = 2; i <= n; i += 2) seq.Add(i);
            for (int i = 1; i <= n; i += 2) seq.Add(i);
        }
        else if (n % 6 == 2)
        {
            // pattern for n mod 6 ==2
            for (int i = 2; i <= n; i += 2) seq.Add(i);
            for (int i = 1; i <= n; i += 2) seq.Add(i);
            // swap first two and move 1 to end adjustments
            if (seq.Count >= 4)
            {
                (seq[0], seq[1]) = (seq[1], seq[0]);
                // move first odd (which became original even) near end to further reduce diagonal clashes
            }
        }
        else // n % 6 == 3
        {
            for (int i = 2; i <= n - 1; i += 2) seq.Add(i);
            for (int i = 1; i <= n - 2; i += 2) seq.Add(i);
            seq.Add(n);
        }
        // Convert to zero-based rows: column index i maps to row seq[i]-1
        var rows = new int[n];
        for (int col = 0; col < n; col++) rows[col] = seq[col] - 1;
        return rows;
    }

    private static IEnumerable<int[]> GenerateSymmetryVariants(int[] rows, int maxVariants)
    {
        var list = new List<int[]>(Math.Min(maxVariants, 7));
        // Board transforms: rotate 90/180/270 and reflect horizontally/vertically/diagonal
        // For large n we only generate a subset until cap reached.
        void AddVariant(int[] r)
        {
            if (list.Count >= maxVariants) return;
            list.Add(r);
        }
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
            for (int c = 0; c < n; c++) r[n - 1 - c] = src[c];
            return r;
        }
        int[] ReflectHorizontal(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++) r[c] = n - 1 - src[c];
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

    // ---------------- Helpers ----------------
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
        _largeBoardRawSolutions.Clear(); // defensive
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private bool ShouldAddSolution() => !_capEnabled || _maxDisplayedCount <= 0 || _solutions.Count < _maxDisplayedCount;

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

    private readonly ISolutionFormatter _formatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly List<int[]> _largeBoardRawSolutions = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled;
    private readonly int _maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private bool _disposed;
    private const int _lookupThreshold = 20; // restored: use lookup starting at N >= 20
    private const int _largeBoardConstructiveThreshold = 20; // constructive sampling threshold
    private void EnumerateAllAdaptive(bool countOnly)
    {
        int cap = countOnly ? 0 : _maxDisplayedCount;
        ulong totalCount = 0;
        int materialized = 0;
        int N = BoardSize;
        if (N <= 0) { _solutionCount = 0; return; }

        int cores = Environment.ProcessorCount;
        int targetJobs = cores * 128;
        int maxDepth = 4;
        int depth = 2;
        double branchEstimate = Math.Max(2.0, N * 0.55);
        while (depth < maxDepth && Math.Pow(branchEstimate, depth) < targetJobs) depth++;

        var partialStates = new List<(int col, int[] rows, ulong cols, ulong d1, ulong d2)>();
        bool abortGen = false;
        int maxStates = targetJobs * 2;

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
            ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            ulong avail = ~(cols | d1 | d2) & mask;
            while (avail != 0 && !abortGen)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                rows[col] = row;
                Gen(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1, rows);
                rows[col] = -1;
            }
        }

        int totalJobs = partialStates.Count;
        if (totalJobs == 0) { _solutionCount = 0; return; }

        int processedJobs = 0;
        int lastReported = -1;
        int progressSpan = 95;

        Parallel.ForEach(partialStates, new ParallelOptions { MaxDegreeOfParallelism = cores }, state =>
        {
            var (startCol, rows, cols, d1, d2) = state;
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
                            if (rows.Length <= 25)
                            {
                                var packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
                                lock (_solutions) _solutions.Add((packed, rows.Length));
                            }
                            else
                            {
                                var copy = new int[rows.Length];
                                Array.Copy(rows, copy, rows.Length);
                                lock (_largeBoardRawSolutions) _largeBoardRawSolutions.Add(copy);
                            }
                            if (EnableEvents && !_eventsSuppressedAfterCap)
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                            if (current == cap) _eventsSuppressedAfterCap = true;
                        }
                    }
                    return;
                }
                ulong maskAll = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
                ulong avail = ~(lc | ld1 | ld2) & maskAll;
                while (avail != 0 && !IsSolverCanceled)
                {
                    ulong bit = avail & (ulong)-(long)avail;
                    avail ^= bit;
                    int row = BitOperations.TrailingZeroCount(bit);
                    rows[col] = row;
                    DFS(col + 1, lc | bit, (ld1 | bit) << 1, (ld2 | bit) >> 1);
                    rows[col] = -1;
                }
            }
            DFS(startCol, cols, d1, d2);
            int done = Interlocked.Increment(ref processedJobs);
            int pct = (int)Math.Min(progressSpan, (double)done / totalJobs * progressSpan);
            if (pct != lastReported)
            {
                int prev = Interlocked.Exchange(ref lastReported, pct);
                if (pct != prev && EnableEvents)
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
            }
        });

        _solutionCount = totalCount;
        if (EnableEvents)
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
}
