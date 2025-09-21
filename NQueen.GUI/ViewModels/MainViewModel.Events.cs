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
        SimulationCompleted?.Invoke(this, EventArgs.Empty);

        // After simulation finishes always visualize the FIRST solution (if any)
        if (ObservableSolutions.Count > 0)
        {
            var first = ObservableSolutions[0];

            // If SelectedSolution was already the first (but hidden during simulation), force a redraw
            if (SelectedSolution == first)
            {
                EnsureBoardSized();
                ChessboardVm?.PlaceQueens(first.Positions);
            }
            else
            {
                // Setting SelectedSolution will trigger OnSelectedSolutionChanged and render
                SelectedSolution = first;
            }
        }
    }

    private void OnProgressValueChanged(ProgressValueChangedMessage message)
    {
        Debug.WriteLine($"[MainViewModel] ProgressValue received: {message.Value}");
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        // Suppress incremental visualization while simulating if Hide mode
        if (DisplayMode == DisplayMode.Hide)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        var span = message.Solution.Span;
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
        AddSolutionToObservable(newSolution);
        NoOfSolutions = $"{ObservableSolutions.Count,0:N0}";

        // Always set SelectedSolution to the first discovered one only.
        if (solutionId == 1)
            SelectedSolution = newSolution;
    }

    private void AddSolutionToObservable(Solution solution)
    {
        if (solution is null)
            return;

        // Prevent late (post-cancel) or stray additions:
        //  - If solver canceled, ignore.
        //  - If simulation not running AND output not finalized (IsOutputReady == false), ignore.
        if (_solver.IsSolverCanceled || (IsSimulating == false && IsOutputReady == false))
            return;

        _uiDispatcher.Invoke(() =>
        {
            if (_solver.IsSolverCanceled || (IsSimulating == false && IsOutputReady == false))
                return;

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

    private void OnSolutionFoundEvent(object? sender, NQueen.Domain.EventArgsPruning.SolutionFoundEventArgs e)
    {
        if (_solver.IsSolverCanceled || IsSimulating == false && IsOutputReady == false)
            return;

        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));
    }

    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        Debug.WriteLine($"[MainViewModel] OnProgressValueChangedEvent: Value={e.Value}");
        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    // Show selected solution after simulation regardless of DisplayMode.
    // Hide suppresses only incremental updates (OnQueenPlaced) while IsSimulating.
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

        // If still simulating and Hide -> do not visualize yet
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

        // Switching modes should not clear existing solution visualization after simulation.
        if (IsSimulating)
        {
            // During simulation: Hide -> just clear; Visualize -> leave incremental logic to events
            if (value == DisplayMode.Hide)
                ChessboardVm?.ClearImages();
            return;
        }

        // Simulation finished: always show currently selected solution (if any)
        if (SelectedSolution != null)
        {
            EnsureBoardSized();
            ChessboardVm?.PlaceQueens(SelectedSolution.Positions);
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