namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // --- Missing private fields (added) ---
    private DispatcherTimer? _visualizeTimer;
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

    // --- Event subscriptions ---
    private void SubscribeToSimulationEvents()
    {
        UnsubscribeFromSimulationEvents();
        if (_solver == null) return;
        _hasProgressTick = false;
        _solver.QueenPlaced += OnQueenPlacedEvent;
        _solver.SolutionFound += OnSolutionFoundEvent;
        _solver.ProgressValueChanged += OnProgressValueChangedEvent;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        if (_solver == null) return;
        _solver.QueenPlaced -= OnQueenPlacedEvent;
        _solver.SolutionFound -= OnSolutionFoundEvent;
        _solver.ProgressValueChanged -= OnProgressValueChangedEvent;
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
        if (_pendingPrefixRows == null || _pendingDepth <= 0)
        {
            StopVisualizationTimer();
            return;
        }

        if (_displayedDepth > _pendingDepth)
        {
            ChessboardVm.ClearImages();
            _displayedDepth = 0;
        }

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

        // Keep timer interval in sync with clamped delay
        if (_visualizeTimer != null)
        {
            int clamped = DelayInMilliseconds <= 0
                ? 0
                : Math.Max(SimulationSettings.MinDelayInMilliseconds, DelayInMilliseconds);
            _visualizeTimer.Interval = TimeSpan.FromMilliseconds(clamped > 0 ? clamped : 1);
        }
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
        _pendingPrefixRows = null;
        _displayedPrefixRows = null;
        _pendingDepth = 0;
        _displayedDepth = 0;
    }

    // --- QueenPlaced event (uses helpers above) ---
    private void OnQueenPlacedEvent(object? sender, QueenPlacedEventArgs e)
    {
        MaybeForceEarlyProgress();

        if (ChessboardVm == null || DisplayMode == DisplayMode.Hide) return;
        EnsureBoardSized();

        var span = e.Solution.Span;
        int boardSize = e.BoardSize > 0
            ? e.BoardSize
            : (ParsingUtils.TryParseInt(BoardSizeText, out var parsed) ? parsed : span.Length);

        int max = Math.Min(boardSize, span.Length);
        int validDepth = ComputeValidDepth(span, max);

        if (validDepth <= 0)
        {
            _uiDispatcher.Invoke(() =>
            {
                ChessboardVm.ClearImages();
                _displayedDepth = 0;
                _displayedPrefixRows = null;
            });
            return;
        }

        bool useTimer = DelayInMilliseconds > 0;

        if (!useTimer)
        {
            int[] snapshot = new int[validDepth];
            for (int i = 0; i < validDepth; i++)
                snapshot[i] = span[i];

            var positions = new List<Position>(validDepth);
            for (int c = 0; c < validDepth; c++)
                positions.Add(new Position(c, snapshot[c]));

            _uiDispatcher.Invoke(() =>
            {
                ChessboardVm.PlaceQueens(positions);
                _displayedDepth = validDepth;
                _displayedPrefixRows = snapshot;
            });
            return;
        }

        int[] throttled = new int[validDepth];
        for (int i = 0; i < validDepth; i++)
            throttled[i] = span[i];

        _uiDispatcher.Invoke(() =>
        {
            EnsureVisualizationTimer();
            _pendingPrefixRows = throttled;
            _pendingDepth = validDepth;
            if (_visualizeTimer != null && !_visualizeTimer.IsEnabled)
                _visualizeTimer.Start();
        });
    }

    // --- Additional events (stubs retained) ---
    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e)
    {
        if (_solver?.IsSolverCanceled == true || !IsSimulating) return;
        if (e.Solution.Length == 0) return;

        int id = _batchedSolutions.Count + 1;
        var arr = e.Solution.ToArray();
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

    private void OnProgressValueChangedEvent(object? sender, ProgressUpdateEventArgs e)
    {
        if (e.SimulationToken != _currentSimulationToken || _solver?.IsSolverCanceled == true)
            return;

        _uiDispatcher.Invoke(() =>
        {
            if (SolutionMode == SolutionMode.Single && DisplayMode == DisplayMode.Visualize)
            {
                ProgressVisibility = Visibility.Visible;
                ProgressLabelVisibility = Visibility.Hidden;
                ProgressLabel = string.Empty;
                return;
            }
            int raw = (int)Math.Round(e.Value);
            SetProgressPercent(raw);
        });
    }

    // --- Missing helper implementations ---
    private void MaybeForceEarlyProgress() => ForceEarlyProgressIfNeeded();

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
