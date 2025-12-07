namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void SolveSingleMode()
    {
        bool visualize = DisplayMode == DisplayMode.Visualize;

        // 1. Curated fast path (skip when visualizing to show real backtracking)
        if (!visualize && NQueen.Domain.Utils.ExpectedSolutionData.SingleSolutions.TryGetValue(BoardSize, out var list) &&
            list.Count > 0)
        {
            var rows = list[0];
            if (!ValidateRows(rows)) return;
            _solutionCount = 1;

            EmitSingleVisualization(rows);
            MaterializeSingle(rows);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // 2. Large board constructive path (skip when visualizing)
        if (!visualize && BoardSize > 25)
        {
            var rows = GenerateConstructiveSingleSolution(BoardSize);
            if (!ValidateRows(rows)) return;
            _solutionCount = 1;

            EmitSingleVisualization(rows);
            MaterializeSingle(rows);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // 3. Fallback enumeration (always used when visualizing to show full backtracking)
        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: false,
            DisplayMode,
            DelayInMillisec: Math.Max(SimulationSettings.MinDelayInMilliseconds, DelayInMillisec),
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m =>
            {
                if (EnableEvents && !_eventsSuppressedAfterCap)
                    QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize));
            },
            rows =>
            {
                if (!ValidateRows(rows)) return false;
                if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && (!_capEnabled || _maxDisplayedCount > 0))
                {
                    _solutionCount = 1;
                    MaterializeSingle(rows);
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                    return true;
                }
                return false;
            }
        ));
    }

    // Emits incremental QueenPlaced events (depth 1..N).
    // Reintroduced delay respect (DelayInMillisec) to allow UI pacing when backend configured.
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
            if (IsSolverCanceled) break;
            prefix[depth - 1] = rows[depth - 1];

            // Snapshot copy (avoid mutation between events)
            var snapshot = new int[n];
            Array.Copy(prefix, snapshot, n);

            QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(new Memory<int>(snapshot), BoardSize));

            if (DelayInMillisec > 0)
            {
                var slept = 0;
                // Responsive sleep with cancellation check every 25ms
                while (slept < DelayInMillisec && !IsSolverCanceled)
                {
                    var step = Math.Min(25, DelayInMillisec - slept);
                    Thread.Sleep(step);
                    slept += step;
                }
                if (IsSolverCanceled) break;
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

        if (EnableEvents && !_eventsSuppressedAfterCap)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
    }

    private static int[] GenerateConstructiveSingleSolution(int n)
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
}
