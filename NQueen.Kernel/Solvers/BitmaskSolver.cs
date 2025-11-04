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
            var sw = Stopwatch.StartNew();
            bool usedLookupAll = false;
            bool usedLookupUnique = false;

            // --- Count-only lookups ---
            if (SolutionMode == SolutionMode.All && allCountOnly)
            {
                ulong c = ExpectedSolutionCounts.GetAll(BoardSize);
                if (c > 0)
                {
                    _solutionCount = c;
                    usedLookupAll = true;
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
                }
            }
            else if (SolutionMode == SolutionMode.Unique && uniqueCountOnly)
            {
                ulong c = ExpectedSolutionCounts.GetUnique(BoardSize);
                if (c > 0)
                {
                    _solutionCount = c;
                    usedLookupUnique = true;
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
                }
            }

            // --- Materialize sample paths (lookup total count, enumerate only up to cap) ---
            if (SolutionMode == SolutionMode.All && !allCountOnly)
            {
                ulong c = ExpectedSolutionCounts.GetAll(BoardSize);
                if (c > 0)
                {
                    _solutionCount = c;
                    usedLookupAll = true;
                    SampleAllMaterializeUsingLookup();
                }
                else
                {
                    // fallback old full enumeration (parallel decision logic remains)
                    bool autoParallel = ParallelSplitDepthHeuristic.ShouldUseParallelForAll(BoardSize);
                    int splitDepth = UseAdaptiveDepth ? ParallelSplitDepthHeuristic.GetOptimalSplitDepth(BoardSize) : ParallelRootSplitDepth;
                    if (autoParallel)
                        RunAllParallel(splitDepth);
                    else
                        RunAllSequential();
                }
            }
            else if (SolutionMode == SolutionMode.Unique && !uniqueCountOnly)
            {
                ulong c = ExpectedSolutionCounts.GetUnique(BoardSize);
                if (c > 0)
                {
                    _solutionCount = c;
                    usedLookupUnique = true;
                    SampleUniqueMaterializeUsingLookup();
                }
                else
                {
                    if (UseParallel)
                        RunUniqueParallel();
                    else
                        RunUniqueSequential();
                }
            }
            else if (SolutionMode == SolutionMode.Single)
            {
                SolveSingleMode();
            }

            sw.Stop();
            var results = BuildResults(sw.Elapsed);
            if (usedLookupAll || usedLookupUnique)
            {
                // Ensure total is authoritative
                return new SimulationResults(results.Solutions, _solutionCount, Math.Round(sw.Elapsed.TotalSeconds, 1));
            }
            return results;
        }
    }

    // Enumerate All solutions only until cap for sample display; total count from lookup.
    private void SampleAllMaterializeUsingLookup()
    {
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        if (cap <= 0) return;
        // Fast constructive path for large boards avoids exhaustive search
        if (BoardSize >= _largeBoardConstructiveThreshold)
        {
            int effectiveCap = _maxDisplayedCount; // always respect display cap for samples
            ConstructiveSampleSolutions(isUnique:false, effectiveCap);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }
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
                if (materialized < cap && ShouldAddSolution())
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
                    materialized++;
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                    if (materialized >= cap)
                    {
                        _eventsSuppressedAfterCap = true;
                        return true; // early terminate search
                    }
                }
                return false;
            }
        ));
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Enumerate Unique solutions only until cap; uniqueness enforced via canonical key set.
    private void SampleUniqueMaterializeUsingLookup()
    {
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        if (cap <= 0) return;
        if (BoardSize >= _largeBoardConstructiveThreshold)
        {
            int effectiveCap = _maxDisplayedCount; // always respect display cap for samples
            ConstructiveSampleSolutions(isUnique:true, effectiveCap);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }
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
                if (rows.Length <= 25)
                    packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
                if (seen.Add(packed))
                {
                    if (rows.Length <= 25)
                        _solutions.Add((packed, rows.Length));
                    else
                    {
                        var copy = new int[rows.Length];
                        Array.Copy(rows, copy, rows.Length);
                        _largeBoardRawSolutions.Add(copy);
                    }
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                    if (seen.Count >= cap)
                    {
                        _eventsSuppressedAfterCap = true;
                        return true; // early terminate
                    }
                }
                return false;
            }
        ));
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Constructive generator using known pattern (even rows then odd rows with adjustments) + symmetry transforms
    private void ConstructiveSampleSolutions(bool isUnique, int cap)
    {
        var baseRows = GenerateConstructiveSolution(BoardSize);
        if (!ValidateRows(baseRows)) return;
        AddMaterialized(baseRows);
        if (cap == 1) return;
        var variants = GenerateSymmetryVariants(baseRows, cap - 1);
        foreach (var v in variants) AddMaterialized(v);

        void AddMaterialized(int[] rows)
        {
            if (_solutions.Count + _largeBoardRawSolutions.Count >= cap) return;
            // For unique mode we deliberately keep symmetry variants as-is (no canonical collapsing)
            if (isUnique)
            {
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

    // Remove deprecated methods entirely to prevent accidental use
    // private void SolveUniqueCountOnlyMode() { }
    // private void SolveAllCountOnlyMode() { }

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

    private bool ShouldAddSolution()
    {
        if (_capEnabled == false) return true;
        return _maxDisplayedCount <= 0 || _solutions.Count < _maxDisplayedCount;
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

    private readonly ISolutionFormatter _formatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly List<int[]> _largeBoardRawSolutions = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled;
    private readonly int _maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private bool _disposed;
    private const int _largeBoardConstructiveThreshold = 20; // switch to O(n) constructive sampling above this
}
