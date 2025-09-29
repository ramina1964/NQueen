namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Batching / state fields
    private List<Solution> _batchedSolutions = new();
    private int _actualTotalSolutions = 0;
    private bool _hasProgressTick;

    // Progress tracking
    private int _lastProgressPercent = 0;
    private bool _progressCompleted = false;

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

    // Called from ManageSimulationStatus(Finished)
    private void OnSimulationCompleted()
    {
        SimulationCompleted?.Invoke(this, EventArgs.Empty);

        if (_batchedSolutions.Count > 0)
        {
            _uiDispatcher.Invoke(() =>
            {
                ObservableSolutions.Clear();
                foreach (var sol in _batchedSolutions)
                    ObservableSolutions.Add(sol);

                if (SimulationResults != null)
                    NoOfSolutions = $"{SimulationResults.SolutionsCount,0:N0}";

                var first = _batchedSolutions[0];
                if (!ReferenceEquals(SelectedSolution, first))
                    SelectedSolution = first;

                RenderSelectedSolution();
            });
        }

        _batchedSolutions.Clear();
        _actualTotalSolutions = 0;
    }

    private void OnSolutionFoundEvent(object? sender, NQueen.Domain.EventArgsPruning.SolutionFoundEventArgs e)
    {
        if (_solver.IsSolverCanceled || (IsSimulating == false && IsOutputReady == false))
            return;

        _actualTotalSolutions++;

        var solutionId = _batchedSolutions.Count + 1;
        var newSolution = new Solution(e.Solution.ToArray(), _solutionFormatter, solutionId);
        _batchedSolutions.Add(newSolution);

        if (solutionId == 1)
        {
            _uiDispatcher.Invoke(() =>
            {
                if (_solver.IsSolverCanceled)
                    return;
                SelectedSolution = newSolution;
            });
        }
    }

    // Progress handling
    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        // Ignore stale or post-finish events
        if (e.SimulationToken != _currentSimulationToken || _solver.IsSolverCanceled || _progressCompleted)
            return;

        _uiDispatcher.Invoke(() =>
        {
            // Single + Visualize stays indeterminate
            if (SolutionMode == SolutionMode.Single && DisplayMode == DisplayMode.Visualize)
            {
                ProgressVisibility = Visibility.Visible;
                ProgressLabelVisibility = Visibility.Hidden;
                ProgressLabel = string.Empty;
                return;
            }

            int raw = (int)Math.Round(e.Value);
            int clamped = Math.Clamp(raw, 0, 100);

            // Prevent first / early overshoot to 100%. Defer 100 until Finished state.
            if (clamped >= 100)
            {
                clamped = 99; // reserve 100% for completion
            }

            // Monotonic progression
            if (clamped < _lastProgressPercent)
            {
                clamped = _lastProgressPercent; // never go backwards
            }
            else
            {
                _lastProgressPercent = clamped;
            }

            ProgressVisibility = Visibility.Visible;
            ProgressLabelVisibility = Visibility.Visible;
            ProgressValue = clamped / 100.0;
            ProgressLabel = $"{clamped}%";

            if (clamped is > 0 and < 100)
                _hasProgressTick = true;

            Debug.WriteLine($"[MainViewModel] Progress event: raw={e.Value}, mapped={clamped}%, last={_lastProgressPercent}%");
        });
    }

    private void MaybeForceEarlyProgress()
    {
        if (_hasProgressTick || IsSingleRunning || _progressCompleted)
            return;

        _hasProgressTick = true;
        _uiDispatcher.Invoke(() =>
        {
            if (ProgressValue < 0.01)
            {
                ProgressVisibility = Visibility.Visible;
                ProgressLabelVisibility = Visibility.Visible;
                ProgressValue = 0.01;
                ProgressLabel = "1%";
                _lastProgressPercent = Math.Max(_lastProgressPercent, 1);
                Debug.WriteLine("[MainViewModel] Forced early progress tick at 1%.");
            }
        });
    }

    private void OnQueenPlacedEvent(object? sender,
        Domain.EventArgsPruning.QueenPlacedEventArgs e)
    {
        MaybeForceEarlyProgress();

        if (DisplayMode == DisplayMode.Hide)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        var memory = e.Solution;
        var span = memory.Span;
        var max = Math.Min(boardSize, span.Length);

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

        if (depth == 0)
        {
            _uiDispatcher.Invoke(() => ChessboardVm.ClearImages());
            return;
        }

        List<Position> positions = new(depth);
        for (int c = 0; c < depth; c++)
            positions.Add(new Position(c, span[c]));

        _uiDispatcher.Invoke(() => ChessboardVm.PlaceQueens(positions));
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

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        // First attempt to clear any stale visualization constraint error
        // (defined in Validation partial; safe no-op if it didn’t exist earlier).
        ClearVisualizationConstraintIfSatisfied();

        if (_solver == null)
            return;

        // Re-run validation (this will also re-set IsValid and command states).
        if (ValidateAndSetUiState() == false)
            return;

        OnPropertyChanged(nameof(BoardSizeText));

        if (IsSimulating)
        {
            if (value == DisplayMode.Hide)
                ChessboardVm?.ClearImages();

            // If switching to Visualize in a single-solution run, keep percentage hidden.
            if (SolutionMode == SolutionMode.Single && value == DisplayMode.Visualize)
            {
                ProgressLabelVisibility = Visibility.Hidden;
                ProgressLabel = string.Empty;
            }
            return;
        }

        // Not simulating: re-render current selection (if any)
        RenderSelectedSolution();
    }

    private void RenderSelectedSolution()
    {
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

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize))
        {
            if (ChessboardVm.Squares.Count == 0 ||
                ChessboardVm.IsBoardStateUpdatedAndSquaresPopulated(boardSize) == false)
            {
                ChessboardVm.CreateSquares(boardSize);
            }
        }
    }
}
