namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void SubscribeToSimulationEvents()
    {
        Debug.WriteLine($"[MainViewModel] Subscribing to: {_solver?.GetHashCode()}");

        // First, unsubscribe from any existing events to avoid duplicates
        UnsubscribeFromSimulationEvents();

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
        UpdateProgress(message.Value, $"{Math.Round(message.Value * 100, 1)} %");
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        var positions = message
            .Solution
            .Take(boardSize)
            .Select((queenPosition, rowIndex) => new Position(rowIndex, queenPosition))
            .ToList();

        ChessboardVm.PlaceQueens(positions);
    }

    private void OnSolutionFound(SolutionFoundMessage message)
    {
        var solutionId = ObservableSolutions.Count + 1;
        var newSolution = new Solution(message.Solution, solutionId);
        UpdateSolutionCount();
        AddSolutionToObservable(newSolution);
        SelectedSolution = newSolution;
    }

    private void UpdateProgress(double value, string label)
    {
        value = Math.Clamp(value, 0, 1);
        _uiDispatcher.Invoke(() =>
        {
            Debug.WriteLine($"[MainViewModel] ProgressValue set to: {value}");
            if (IsSingleRunning == false)
            {
                ProgressValue = value;
                ProgressLabel = label;
            }
        });
    }

    private void UpdateSolutionCount() =>
        NoOfSolutions = NumericUtility.IncrementFormattedNumber(NoOfSolutions);

    private void AddSolutionToObservable(Solution solution)
    {
        if (ObservableSolutions.Any(existingSol => existingSol.Id == solution.Id))
            return;

        _uiDispatcher.Invoke(() =>
        {
            if (ObservableSolutions.Count >= SimulationSettings.MaxNoOfSolutionsInOutput)
                ObservableSolutions.RemoveAt(0);
            ObservableSolutions.Add(solution);
        });
    }

    private void OnQueenPlacedEvent(object? sender, QueenPlacedEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution, 0));

    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));

    private void OnProgressValueChangedEvent(object? sender,
        ProgressValueChangedWithTokenEventArgs e)
    {
        Debug.WriteLine($"[MainViewModel] OnProgressValueChangedEvent: Received Token={e.SimulationToken}, Current Token={_currentSimulationToken}");
        if (e.SimulationToken != _currentSimulationToken)
            return;

        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }
}
