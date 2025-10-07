namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Batching / state fields (unchanged for solution list logic)
    private readonly List<Solution> _batchedSolutions = [];
    private int _actualTotalSolutions;
    private bool _hasProgressTick;

    // Visualization pacing (REFACTORED)
    private DispatcherTimer? _visualizeTimer;
    private int[]? _pendingPrefixRows;
    private int _pendingDepth;
    private int[]? _displayedPrefixRows;
    private int _displayedDepth;

    private void EnsureVisualizationTimer()
    {
        if (_visualizeTimer != null)
            return;

        _visualizeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(1, DelayInMilliseconds))
        };
        _visualizeTimer.Tick += VisualizationTimer_Tick;
    }

    private void VisualizationTimer_Tick(object? sender, EventArgs e)
    {
        if (DisplayMode != DisplayMode.Visualize ||
            !IsSimulating ||
            _pendingPrefixRows == null ||
            _pendingDepth < 0 ||
            ChessboardVm == null)
        {
            StopVisualizationTimer();
            return;
        }

        if (_displayedPrefixRows != null &&
            _displayedDepth == _pendingDepth &&
            RowsEqual(_displayedPrefixRows, _pendingPrefixRows, _pendingDepth))
            return;

        RenderPrefix(_pendingPrefixRows, _pendingDepth);

        if (_visualizeTimer != null)
            _visualizeTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, DelayInMilliseconds));
    }

    private static bool RowsEqual(int[] a, int[] b, int depth)
    {
        if (a.Length < depth || b.Length < depth) return false;
        for (int i = 0; i < depth; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void RenderPrefix(int[] prefixRows, int depth)
    {
        if (ChessboardVm == null)
            return;

        if (depth <= 0)
        {
            ChessboardVm.ClearImages();
            _displayedPrefixRows = null;
            _displayedDepth = 0;
            return;
        }

        List<Position> positions = new(depth);
        for (int c = 0; c < depth; c++)
        {
            var row = prefixRows[c];
            if (row < 0) break;
            positions.Add(new Position(c, row));
        }

        ChessboardVm.PlaceQueens(positions);
        _displayedPrefixRows = prefixRows;
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

    private void SubscribeToSimulationEvents()
    {
        Debug.WriteLine($"[MainViewModel] Subscribing to solver: {_solver?.GetHashCode()}");
        UnsubscribeFromSimulationEvents();

        if (_solver == null)
            return;

        _hasProgressTick = false;
        _solver.QueenPlaced += OnQueenPlacedEvent;
        _solver.SolutionFound += OnSolutionFoundEvent;
        _solver.ProgressValueChanged += OnProgressValueChangedEvent;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        if (_solver == null)
            return;

        _solver.QueenPlaced -= OnQueenPlacedEvent;
        _solver.SolutionFound -= OnSolutionFoundEvent;
        _solver.ProgressValueChanged -= OnProgressValueChangedEvent;
    }

    private void OnSimulationCompleted()
    {
        SimulationCompleted?.Invoke(this, EventArgs.Empty);

        if (DisplayMode == DisplayMode.Hide && _batchedSolutions.Count > 0)
        {
            _uiDispatcher.Invoke(() =>
            {
                ObservableSolutions.Clear();
                foreach (var sol in _batchedSolutions)
                    ObservableSolutions.Add(sol);

                if (SimulationResults != null)
                    NoOfSolutions = $"{SimulationResults.SolutionsCount,0:N0}";

                if (_batchedSolutions.Count > 0)
                {
                    var first = _batchedSolutions[0];
                    if (!ReferenceEquals(SelectedSolution, first))
                        SelectedSolution = first;

                    RenderSelectedSolution();
                }
            });
        }

        _batchedSolutions.Clear();
        _actualTotalSolutions = 0;
        StopVisualizationTimer();
    }

    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e)
    {
        if (_solver?.IsSolverCanceled == true || !IsSimulating)
            return;

        _actualTotalSolutions++;

        var solutionId = _batchedSolutions.Count + 1;
        var newSolution = new Solution(e.Solution.ToArray(), _solutionFormatter, solutionId);
        _batchedSolutions.Add(newSolution);

        if (DisplayMode == DisplayMode.Visualize)
        {
            _uiDispatcher.Invoke(() =>
            {
                if (!IsSimulating || DisplayMode != DisplayMode.Visualize) return;

                var cap = SimulationSettings.MaxNoOfSolutionsInOutput;
                bool underCap = cap <= 0 || ObservableSolutions.Count < cap;
                if (underCap)
                    ObservableSolutions.Add(newSolution);
                SelectedSolution = newSolution;
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

    private static int ComputeValidDepth(Span<int> span, int max)
    {
        int depth = 0;
        for (int col = 0; col < max; col++)
        {
            int row = span[col];
            if (row < 0)
                break;

            bool conflict = false;
            for (int prev = 0; prev < col; prev++)
            {
                int prow = span[prev];
                if (prow == row || Math.Abs(prow - row) == col - prev)
                {
                    conflict = true;
                    break;
                }
            }
            if (conflict)
                break;

            depth = col + 1;
        }
        return depth;
    }

    private void MaybeForceEarlyProgress() => ForceEarlyProgressIfNeeded();

    private void OnQueenPlacedEvent(object? sender,
        QueenPlacedEventArgs e)
    {
        MaybeForceEarlyProgress();

        if (DisplayMode == DisplayMode.Hide || ChessboardVm == null)
            return;

        if (!ParsingUtils.TryParseInt(BoardSizeText, out var boardSize))
            return;

        var span = e.Solution.Span;
        var max = Math.Min(boardSize, span.Length);
        int validDepth = ComputeValidDepth(span, max);

        // Build snapshot OUTSIDE dispatcher to avoid capturing ref struct span in lambda
        if (DelayInMilliseconds <= 0)
        {
            if (validDepth == 0)
            {
                _uiDispatcher.Invoke(() =>
                {
                    ChessboardVm.ClearImages();
                    _displayedDepth = 0;
                    _displayedPrefixRows = null;
                });
                return;
            }

            int[] snapshotRows = new int[validDepth];
            for (int i = 0; i < validDepth; i++)
                snapshotRows[i] = span[i];

            List<Position> positions = new(validDepth);
            for (int c = 0; c < validDepth; c++)
                positions.Add(new Position(c, snapshotRows[c]));

            _uiDispatcher.Invoke(() =>
            {
                ChessboardVm.PlaceQueens(positions);
                _displayedDepth = validDepth;
                _displayedPrefixRows = snapshotRows;
            });
            return;
        }

        // Throttled path
        int[] throttledSnapshot = new int[validDepth];
        for (int i = 0; i < validDepth; i++)
            throttledSnapshot[i] = span[i];

        _uiDispatcher.Invoke(() =>
        {
            EnsureVisualizationTimer();
            _pendingPrefixRows = throttledSnapshot;
            _pendingDepth = validDepth;

            if (_visualizeTimer != null && !_visualizeTimer.IsEnabled)
                _visualizeTimer.Start();
        });
    }

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (ChessboardVm == null)
            return;
        if (value == null)
        {
            ChessboardVm.ClearImages();
            return;
        }

        EnsureBoardSized();
        if (IsSimulating && DisplayMode == DisplayMode.Hide)
            return;

        ChessboardVm.PlaceQueens(value.Positions);
    }

    private void RenderSelectedSolution()
    {
        if (ChessboardVm == null)
            return;
        if (SelectedSolution == null)
        {
            ChessboardVm.ClearImages();
            return;
        }

        EnsureBoardSized();
        ChessboardVm.PlaceQueens(SelectedSolution.Positions);
    }

    private void EnsureBoardSized()
    {
        if (ChessboardVm == null)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) &&
            (ChessboardVm.Squares.Count == 0 ||
             ChessboardVm.IsBoardStateUpdatedAndSquaresPopulated(boardSize) == false))
        {
            ChessboardVm.CreateSquares(boardSize);
        }
    }
}
