namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Batching of solutions discovered during simulation
    private List<Solution> _batchedSolutions = new();
    private int _actualTotalSolutions = 0;

    private void SubscribeToSimulationEvents()
    {
        Debug.WriteLine($"[MainViewModel] Subscribing to solver: {_solver?.GetHashCode()}");

        UnsubscribeFromSimulationEvents();

        if (_solver == null)
            return;

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

    // Called by ManageSimulationStatus when transitioning to Finished
    private void OnSimulationCompleted()
    {
        SimulationCompleted?.Invoke(this, EventArgs.Empty);

        if (_batchedSolutions.Count > 0)
        {
            _uiDispatcher.Invoke(() =>
            {
                // Replace list with full batched list
                ObservableSolutions.Clear();
                foreach (var sol in _batchedSolutions)
                    ObservableSolutions.Add(sol);

                if (SimulationResults != null)
                    NoOfSolutions = $"{SimulationResults.SolutionsCount,0:N0}";

                var first = _batchedSolutions[0];

                // Ensure SelectedSolution references the instance now stored in ObservableSolutions
                if (!ReferenceEquals(SelectedSolution, first))
                    SelectedSolution = first;

                // Unconditionally render the (already) selected solution after simulation completed.
                RenderSelectedSolution();
            });
        }

        _batchedSolutions.Clear();
        _actualTotalSolutions = 0;
    }

    #region Solver Event Handlers (direct, no messenger)

    private void OnSolutionFoundEvent(object? sender, NQueen.Domain.EventArgsPruning.SolutionFoundEventArgs e)
    {
        if (_solver.IsSolverCanceled || (IsSimulating == false && IsOutputReady == false))
            return;

        // Pure data work can stay on worker thread
        _actualTotalSolutions++;

        var solutionId = _batchedSolutions.Count + 1;
        var newSolution = new Solution(e.Solution.ToArray(), _solutionFormatter, solutionId);
        _batchedSolutions.Add(newSolution);

        // UI-affecting property set must be on UI thread
        if (solutionId == 1)
        {
            _uiDispatcher.Invoke(() =>
            {
                // Guard again in case of cancel between enqueue & invoke
                if (_solver.IsSolverCanceled)
                    return;
                SelectedSolution = newSolution;
            });
        }
    }

    private void OnQueenPlacedEvent(object? sender, NQueen.Domain.EventArgsPruning.QueenPlacedEventArgs e)
    {
        if (DisplayMode == DisplayMode.Hide)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        // Work on the span locally; do not capture it in a lambda.
        var mem = e.Solution;              // Memory<int>
        var span = mem.Span;               // Span<int>
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

        // Prepare positions BEFORE dispatcher call so lambda doesn't capture Span<T>.
        List<Position> positions = new(depth);
        for (int c = 0; c < depth; c++)
            positions.Add(new Position(c, span[c]));

        _uiDispatcher.Invoke(() =>
        {
            ChessboardVm.PlaceQueens(positions);
        });
    }

    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        _uiDispatcher.Invoke(() =>
        {
            int progressInt = Math.Clamp((int)e.Value, 0, 100);
            double progressDouble = progressInt / 100.0;
            ProgressVisibility = Visibility.Visible;
            ProgressLabelVisibility = Visibility.Visible;
            ProgressValue = progressDouble;
            ProgressLabel = $"{progressInt}%";
            Debug.WriteLine($"[MainViewModel] Progress event: raw={e.Value}, mapped={progressInt}%");
        });
    }

    #endregion

    // SelectedSolution change hook (auto-generated partial)
    // Suppressed during simulation only if Hide mode to avoid premature rendering.
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
        if (_solver == null)
            return;

        if (ValidateAndSetUiState() == false)
            return;

        OnPropertyChanged(nameof(BoardSizeText));

        if (IsSimulating)
        {
            if (value == DisplayMode.Hide)
                ChessboardVm?.ClearImages();
            return;
        }

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