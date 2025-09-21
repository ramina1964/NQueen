namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void SubscribeToSimulationEvents()
    {
        Debug.WriteLine($"[MainViewModel] Subscribing to: {_solver?.GetHashCode()}");

        UnsubscribeFromSimulationEvents();

        if (_solver == null)
            return;

        // The solver (Bitmask) raises events with args in NQueen.Domain.EventArgsPruning
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
        
        // If message.Value now already in [0..100]; convert to 0..1 for ProgressBar.
        // UpdateProgress(message.Value / 100.0, $"{message.Value:0.#}%");
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        // Suppress incremental visualization when hidden
        if (DisplayMode == DisplayMode.Hide)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        var solutionSpan = message.Solution.Span;
        var length = Math.Min(boardSize, solutionSpan.Length);
        var positions = new List<Position>(length);
        for (int colIndex = 0; colIndex < length; colIndex++)
        {
            int queenRow = solutionSpan[colIndex];
            positions.Add(new Position(colIndex, queenRow));
        }

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
            // Reject duplicates (Id uniqueness is assumed stable)
            if (ObservableSolutions.Any(s => s.Id == solution.Id))
                return;

            int cap = SimulationSettings.MaxNoOfSolutionsInOutput;

            // cap <= 0 means "no cap" (per comment in SimulationSettings)
            if (cap > 0 && ObservableSolutions.Count >= cap)
                return; // Do not add; keep oldest solutions

            ObservableSolutions.Add(solution);
        });
    }

    // Fully qualify event arg types to match Bitmask solver namespace (EventArgsPruning)
    private void OnQueenPlacedEvent(object? sender, NQueen.Domain.EventArgsPruning.QueenPlacedEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution, 0));

    private void OnSolutionFoundEvent(object? sender, NQueen.Domain.EventArgsPruning.SolutionFoundEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));

    private void OnProgressValueChangedEvent(object? sender, NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs e)
    {
        Debug.WriteLine($"[MainViewModel] OnProgressValueChangedEvent: Value={e.Value}");
        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    private void RefreshVisualization()
    {
        if (ChessboardVm == null)
            return;

        if (DisplayMode == DisplayMode.Hide)
        {
            ChessboardVm.ClearImages();
            return;
        }

        if (SelectedSolution != null)
            ChessboardVm.PlaceQueens(SelectedSolution.Positions);
    }
}