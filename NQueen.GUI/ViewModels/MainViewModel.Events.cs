namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Batching / state fields
    private List<Solution> _batchedSolutions = new();
    private int _actualTotalSolutions = 0;
    private bool _hasProgressTick;

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
        if (_solver.IsSolverCanceled || IsSimulating == false)
            return;

        _actualTotalSolutions++;

        var solutionId = _batchedSolutions.Count + 1;
        var newSolution = new Solution(e.Solution.ToArray(), _solutionFormatter, solutionId);
        _batchedSolutions.Add(newSolution);

        if (DisplayMode == DisplayMode.Visualize)
        {
            _uiDispatcher.Invoke(() =>
            {
                var cap = SimulationSettings.MaxNoOfSolutionsInOutput;
                bool underCap = cap <= 0 || ObservableSolutions.Count < cap;
                if (underCap)
                    ObservableSolutions.Add(newSolution);
                SelectedSolution = newSolution;
            });
        }
    }

    // Progress handling
    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        if (e.SimulationToken != _currentSimulationToken || _solver.IsSolverCanceled)
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

    private void MaybeForceEarlyProgress() => ForceEarlyProgressIfNeeded();

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
        ClearVisualizationConstraintIfSatisfied();

        if (_solver == null)
            return;

        if (ValidateAndSetUiState() == false)
            return;

        OnPropertyChanged(nameof(BoardSizeText));

        if (IsSimulating)
        {
            if (value == DisplayMode.Hide)
                ChessboardVm?.ClearImages();

            if (SolutionMode == SolutionMode.Single && value == DisplayMode.Visualize)
            {
                ProgressLabelVisibility = Visibility.Hidden;
                ProgressLabel = string.Empty;
            }
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
