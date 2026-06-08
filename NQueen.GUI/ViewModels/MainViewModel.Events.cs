namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // --- Missing private fields (added) ---
    private DispatcherTimer? _visualizeTimer;
    private ChannelReader<QueenPlacedInfo>? _queenPlacedReader;
    private int[]? _pendingPrefixRows;
    private int _pendingDepth;
    private int[]? _displayedPrefixRows;
    private int _displayedDepth;
    private readonly List<Solution> _batchedSolutions = [];
    private bool _hasProgressTick;

    // Enforce domain min and propagate to solver + timer
    partial void OnDelayInMillisecondsChanged(int value)
    {
        // Allow 0 to mean "no delay"; otherwise clamp to domain min (5ms)
        int clamped = value <= 0 ? 0 : Math.Max(SimulationSettings.MinDelayInMilliseconds, value);
        if (clamped != value)
        {
            _delayInMilliseconds = clamped;
            OnPropertyChanged(nameof(DelayInMilliseconds));
        }

        if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
            b.DelayInMillisec = clamped;

        if (_visualizeTimer != null)
            _visualizeTimer.Interval = TimeSpan.FromMilliseconds(clamped > 0 ? clamped : 1);
    }

    // --- Visualization timer helpers ---
    private void EnsureVisualizationTimer()
    {
        if (_visualizeTimer != null) return;

        int clamped = DelayInMilliseconds <= 0
            ? 0
            : Math.Max(SimulationSettings.MinDelayInMilliseconds, DelayInMilliseconds);

        _visualizeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(clamped > 0 ? clamped : 1)
        };
        _visualizeTimer.Tick += VisualizationTimer_Tick;
    }

    private void VisualizationTimer_Tick(object? sender, EventArgs e)
    {
        if (DisplayMode != DisplayMode.Visualize || !IsSimulating || ChessboardVm == null)
        {
            StopVisualizationTimer();
            return;
        }

        // Pull the most recent prefix the solver produced since the last tick (keep-latest).
        DrainQueenPlacedChannel();

        if (_pendingPrefixRows == null || _pendingDepth <= 0)
        {
            // Nothing to show yet; keep the timer alive for the rest of the run so later
            // placements are still drained and rendered.
            SyncTimerInterval();
            return;
        }

        if (_displayedDepth > _pendingDepth)
        {
            ChessboardVm.ClearImages();
            _displayedDepth = 0;
        }

        if (DelayInMilliseconds > 0)
        {
            // Animated path: advance one column per tick for a smooth placement effect.
            if (_displayedDepth < _pendingDepth)
            {
                int nextDepth = _displayedDepth + 1;
                RenderPrefix(_pendingPrefixRows, nextDepth);
                _displayedPrefixRows = _pendingPrefixRows;
                _displayedDepth = nextDepth;
            }
            else if (_displayedPrefixRows == null ||
                     !RowsEqual(_displayedPrefixRows, _pendingPrefixRows, _pendingDepth))
            {
                RenderPrefix(_pendingPrefixRows, _pendingDepth);
            }
        }
        else if (_displayedPrefixRows == null ||
                 _displayedDepth != _pendingDepth ||
                 !RowsEqual(_displayedPrefixRows, _pendingPrefixRows, _pendingDepth))
        {
            // No-delay path: render the full latest prefix immediately (timer throttles to ~1ms).
            RenderPrefix(_pendingPrefixRows, _pendingDepth);
        }

        SyncTimerInterval();
    }

    private static bool RowsEqual(int[] a, int[] b, int depth)
    {
        if (a.Length < depth || b.Length < depth) return false;
        for (int i = 0; i < depth; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void RenderPrefix(int[] rows, int depth)
    {
        if (ChessboardVm == null) return;
        if (depth <= 0)
        {
            ChessboardVm.ClearImages();
            _displayedPrefixRows = null;
            _displayedDepth = 0;
            return;
        }

        var positions = new List<Position>(depth);
        for (int c = 0; c < depth; c++)
        {
            int r = rows[c];
            if (r < 0) break;
            positions.Add(new Position(c, r));
        }

        ChessboardVm.PlaceQueens(positions);
        _displayedPrefixRows = rows;
        _displayedDepth = depth;
    }

    private void StopVisualizationTimer()
    {
        if (_visualizeTimer != null)
        {
            _visualizeTimer.Stop();
            _visualizeTimer.Tick -= VisualizationTimer_Tick;
            _visualizeTimer = null;
        }
        _queenPlacedReader = null;
        _pendingPrefixRows = null;
        _displayedPrefixRows = null;
        _pendingDepth = 0;
        _displayedDepth = 0;
    }

    // --- QueenPlaced channel drain (replaces the former QueenPlaced event handler) ---
    // The solver writes the latest partial prefix to a conflating Channel<QueenPlacedInfo>
    // (capacity 1, drop-oldest). StartQueenPlacedDrain wires the reader and starts the timer;
    // DrainQueenPlacedChannel runs on the UI thread from each timer tick, reads to the most recent
    // value, and stages it into _pendingPrefixRows/_pendingDepth for the render logic above.
    private void StartQueenPlacedDrain(ChannelReader<QueenPlacedInfo> reader)
    {
        _queenPlacedReader = reader;
        EnsureVisualizationTimer();
        if (_visualizeTimer != null && !_visualizeTimer.IsEnabled)
            _visualizeTimer.Start();
    }

    private void DrainQueenPlacedChannel()
    {
        var reader = _queenPlacedReader;
        if (reader == null) return;

        bool drainedAny = false;
        QueenPlacedInfo latest = default;
        while (reader.TryRead(out var info))
        {
            latest = info;
            drainedAny = true;
        }

        if (!drainedAny) return;

        ForceEarlyProgressIfNeeded();

        if (ChessboardVm == null || DisplayMode == DisplayMode.Hide) return;
        EnsureBoardSized();

        var span = latest.Solution.Span;
        int boardSize = latest.BoardSize > 0
            ? latest.BoardSize
            : (ParsingUtils.TryParseInt(BoardSizeText, out var parsed) ? parsed : span.Length);

        int max = Math.Min(boardSize, span.Length);
        int validDepth = ComputeValidDepth(span, max);

        if (validDepth <= 0)
        {
            ChessboardVm.ClearImages();
            _displayedDepth = 0;
            _displayedPrefixRows = null;
            _pendingPrefixRows = null;
            _pendingDepth = 0;
            return;
        }

        int[] snapshot = new int[validDepth];
        for (int i = 0; i < validDepth; i++)
            snapshot[i] = span[i];

        _pendingPrefixRows = snapshot;
        _pendingDepth = validDepth;
    }

    private void SyncTimerInterval()
    {
        if (_visualizeTimer == null) return;
        int clamped = DelayInMilliseconds <= 0
            ? 0
            : Math.Max(SimulationSettings.MinDelayInMilliseconds, DelayInMilliseconds);
        _visualizeTimer.Interval = TimeSpan.FromMilliseconds(clamped > 0 ? clamped : 1);
    }

    // --- Solution sink callback (replaces the former SolutionFound event handler) ---
    // Invoked synchronously on the solver thread through the per-run SynchronousProgress<T> built in
    // SimulateAsync. Synchronous-by-design: info.Solution wraps a reused DFS buffer, so it must be
    // copied (ToArray) before control returns to the solver. UI-collection mutations are marshalled
    // through _uiDispatcher exactly as the event handler did.
    private void OnSolutionFoundReported(SolutionFoundInfo info)
    {
        if (CancellationTokenSource?.IsCancellationRequested == true || !IsSimulating) return;
        if (info.Solution.Length == 0) return;

        int id = _batchedSolutions.Count + 1;
        var arr = info.Solution.ToArray();
        var sol = new Solution(arr, _solutionFormatter, id);
        _batchedSolutions.Add(sol);

        if (DisplayMode == DisplayMode.Visualize)
        {
            _uiDispatcher.Invoke(() =>
            {
                if (!IsSimulating) return;
                if (ObservableSolutions.Count < SimulationSettings.MaxDisplayedCount || SimulationSettings.MaxDisplayedCount <= 0)
                    ObservableSolutions.Add(sol);
                SelectedSolution = sol;
            });
        }
    }

    // --- Progress sink callback (replaces the former ProgressValueChanged event handler) ---
    // Invoked through the per-run Progress<ProgressInfo> built in SimulateAsync. The Guid
    // run-correlation token is gone: a fresh sink is created per simulation, so a previous
    // run's callbacks can no longer reach this instance.
    private void OnProgressReported(ProgressInfo info)
    {
        if (CancellationTokenSource?.IsCancellationRequested == true)
            return;

        _uiDispatcher.Invoke(() =>
        {
            if (SolutionMode == SolutionMode.Single && DisplayMode == DisplayMode.Visualize)
            {
                ProgressVisibility = Visibility.Visible;
                ProgressLabelVisibility = Visibility.Collapsed;
                ProgressLabel = string.Empty;
                return;
            }
            int raw = (int)Math.Round(info.Percent);
            SetProgressPercent(raw);
        });
    }

    private static int ComputeValidDepth(Span<int> span, int max)
    {
        int depth = 0;
        for (int col = 0; col < max; col++)
        {
            int row = span[col];
            if (row < 0) break;

            for (int prev = 0; prev < col; prev++)
            {
                int prow = span[prev];
                if (prow == row || Math.Abs(prow - row) == col - prev)
                    return depth; // conflict: stop at current depth
            }

            depth = col + 1;
        }
        return depth;
    }

    private void EnsureBoardSized()
    {
        if (ChessboardVm == null) return;
        if (!ParsingUtils.TryParseInt(BoardSizeText, out var boardSize)) return;

        if (ChessboardVm.Squares.Count == 0 ||
            !ChessboardVm.IsBoardStateUpdatedAndSquaresPopulated(boardSize))
        {
            ChessboardVm.CreateSquares(boardSize);
        }
    }
}
