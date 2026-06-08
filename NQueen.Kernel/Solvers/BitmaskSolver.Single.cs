namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void SolveSingleMode()
    {
        bool visualize = DisplayMode == DisplayMode.Visualize;

        if (visualize)
        {
            // Engine-backed visualization: emit placements/removals and honor DelayInMillisec
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec: Math.Max(0, DelayInMillisec),
                () => IsCancellationRequested,
                RaiseProgress,
                m => RaiseQueenPlaced(m, BoardSize),
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && (!_capEnabled || _maxDisplayedCount > 0))
                    {
                        _solutionCount = 1;
                        MaterializeSingle(rows);
                        RaiseSolutionFound(rows, BoardSize);
                        return true;
                    }
                    return false;
                }
            ));
            return;
        }

        // 1. Curated fast path (non-visual)
        if (NQueen.Domain.Utils.ExpectedSolutionData.SingleSolutions.TryGetValue(BoardSize, out var list) &&
            list.Count > 0)
        {
            var rows = list[0];
            if (!ValidateRows(rows)) return;
            _solutionCount = 1;

            EmitSingleVisualization(rows);
            MaterializeSingle(rows);
            RaiseProgress(100.0);
            return;
        }

        // 2. Constructive path for medium/large boards when not visualizing
        if (BoardSize >= SimulationSettings.LargeBoardIntermediateStartSize)
        {
            var rows = GenerateConstructiveSolution(BoardSize);
            if (!ValidateRows(rows)) return;
            _solutionCount = 1;

            EmitSingleVisualization(rows);
            MaterializeSingle(rows);
            RaiseProgress(100.0);
            return;
        }

        // 3. Fallback enumeration (non-visual only)
        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: false,
            DisplayMode,
            DelayInMillisec: 0,
            () => IsCancellationRequested,
            RaiseProgress,
            m => RaiseQueenPlaced(m, BoardSize),
            rows =>
            {
                if (!ValidateRows(rows)) return false;
                if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && (!_capEnabled || _maxDisplayedCount > 0))
                {
                    _solutionCount = 1;
                    MaterializeSingle(rows);
                    RaiseSolutionFound(rows, BoardSize);
                    return true;
                }
                return false;
            }
        ));
    }

    private void EmitSingleVisualization(int[] rows)
    {
        if (!EnableEvents ||
            DisplayMode != DisplayMode.Visualize ||
            rows is null ||
            rows.Length == 0)
            return;

        int n = rows.Length;
        var prefix = new int[n];
        Array.Fill(prefix, -1);

        for (int depth = 1; depth <= n; depth++)
        {
            if (IsCancellationRequested) break;
            prefix[depth - 1] = rows[depth - 1];

            // Snapshot copy (avoid mutation between events)
            var snapshot = new int[n];
            Array.Copy(prefix, snapshot, n);

            RaiseQueenPlaced(new Memory<int>(snapshot), BoardSize);

            if (DelayInMillisec > 0)
            {
                var slept = 0;
                // Responsive sleep with cancellation check every 10ms; cap max total wait per depth
                int maxPerDepth = Math.Min(DelayInMillisec, 25);
                while (slept < maxPerDepth && !IsCancellationRequested)
                {
                    var step = Math.Min(10, maxPerDepth - slept);
                    Thread.Sleep(step);
                    slept += step;
                }
                if (IsCancellationRequested) break;
            }
        }
    }

    private void MaterializeSingle(int[] rows)
    {
        if (_solutions.Count != 0 || _largeBoardRawSolutions.Count != 0) return;
        if (_capEnabled && _maxDisplayedCount <= 0) return;

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

        RaiseSolutionFound(rows, BoardSize);
    }

    }
