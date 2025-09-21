namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void SubscribeToSimulationEvents()
    {
        Debug.WriteLine($"[MainViewModel] Subscribing to: {_solver?.GetHashCode()}");

        UnsubscribeFromSimulationEvents();

        if (_solver == null)
            return;

        _solver.QueenPlaced += OnQueenPlacedEvent;
        _solver.SolutionFound += OnSolutionFoundEvent;
        _solver.ProgressValueChanged += OnProgressValueChangedEvent;

        WeakReferenceMessenger.Default.Register<QueenPlacedMessage>(this, (r, m) =>
            OnQueenPlaced(m));

        WeakReferenceMessenger.Default.Register<SolutionFoundMessage>(this, (r, m) =>
            OnSolutionFound(m));

        WeakReferenceMessenger.Default.Register<ProgressValueChangedMessage>(this, (r, m) =>
            OnProgressValueChanged(m));
    }

    private void UnsubscribeFromSimulationEvents()
    {
        if (_solver != null)
        {
            _solver.QueenPlaced -= OnQueenPlacedEvent;
            _solver.SolutionFound -= OnSolutionFoundEvent;
            _solver.ProgressValueChanged -= OnProgressValueChangedEvent;
        }

        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
    }

    private void OnSimulationCompleted()
    {
        UpdateSolutionCount();
        SimulationCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnProgressValueChanged(ProgressValueChangedMessage message)
    {
        Debug.WriteLine($"[MainViewModel] ProgressValue received: {message.Value}");
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        // Suppress incremental visualization when hidden
        if (DisplayMode == DisplayMode.Hide)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        var span = message.Solution.Span;
        var max = Math.Min(boardSize, span.Length);

        // Heuristic to determine current active (valid) depth of the partial solution.
        // The solver reuses the same queenRows array and does NOT clear deeper columns when backtracking,
        // so stale (now invalid) placements can remain beyond the current search depth.
        // We build the longest valid prefix (no row or diagonal conflicts). Anything after the first conflict
        // is considered stale and ignored for visualization to prevent “extra” queens remaining on the board.
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
                // same row or diagonal clash -> stale remainder starts here
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
            ChessboardVm.ClearImages();
            return;
        }

        List<Position> positions = new(depth);
        for (int c = 0; c < depth; c++)
            positions.Add(new Position(c, span[c]));

        ChessboardVm.PlaceQueens(positions);
    }

    private void OnSolutionFound(SolutionFoundMessage message)
    {
        var solutionId = ObservableSolutions.Count + 1;
        var newSolution = new Solution(message.Solution.ToArray(), _solutionFormatter, solutionId);
        UpdateSolutionCount();
        AddSolutionToObservable(newSolution);
        SelectedSolution = newSolution;
    }

    private void UpdateSolutionCount() =>
        NoOfSolutions = NumericUtils.IncFormattedNumber(NoOfSolutions);

    private void AddSolutionToObservable(Solution solution)
    {
        if (solution is null)
            return;

        _uiDispatcher.Invoke(() =>
        {
            if (ObservableSolutions.Any(s => s.Id == solution.Id))
                return;

            int cap = SimulationSettings.MaxNoOfSolutionsInOutput;
            if (cap > 0 && ObservableSolutions.Count >= cap)
                return;

            ObservableSolutions.Add(solution);
        });
    }

    private void OnQueenPlacedEvent(object? sender, NQueen.Domain.EventArgsPruning.QueenPlacedEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution, 0));

    private void OnSolutionFoundEvent(object? sender, NQueen.Domain.EventArgsPruning.SolutionFoundEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));

    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        Debug.WriteLine($"[MainViewModel] OnProgressValueChangedEvent: Value={e.Value}");
        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    // Always show selected solution (even if DisplayMode == Hide) so user can inspect results after simulation.
    // Hide mode suppresses only live incremental visualization (OnQueenPlaced).
    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (ChessboardVm == null)
            return;

        if (value == null)
        {
            ChessboardVm.ClearImages();
            return;
        }

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize))
        {
            if (ChessboardVm.Squares.Count == 0 ||
                ChessboardVm.IsBoardStateUpdatedAndSquaresPopulated(boardSize) == false)
            {
                ChessboardVm.CreateSquares(boardSize);
            }
        }

        ChessboardVm.PlaceQueens(value.Positions);
    }

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        if (_solver == null)
            return;

        if (ValidateAndSetUiState() == false)
            return;

        OnPropertyChanged(nameof(BoardSizeText));

        // If a solution is available, re-render (or clear for live simulation only)
        if (IsOutputReady && SelectedSolution != null)
        {
            if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize))
                ChessboardVm?.CreateSquares(boardSize);

            // We still show the selected solution even if Hide,
            // because Hide only suppresses live incremental placement.
            ChessboardVm?.PlaceQueens(SelectedSolution.Positions);
        }
        else
        {
            UpdateUiState();
        }
    }

    private void RefreshVisualization()
    {
        if (ChessboardVm == null)
            return;

        if (SelectedSolution == null)
        {
            ChessboardVm.ClearImages();
            return;
        }

        ChessboardVm.PlaceQueens(SelectedSolution.Positions);
    }
}