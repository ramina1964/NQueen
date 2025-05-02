namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    public event EventHandler? SimulationCompleted;

    private void OnProgressValueChanged(ProgressValueChangedMessage message) =>
        UpdateProgress(message.Value, $"{message.Value} %");

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        var boardSize = GetBoardSize();
        var positions = message.Solution
            .Take(boardSize)
            .Select((queenPosition, rowIndex) => new Position(rowIndex, queenPosition))
            .ToList();

        ChessboardVm.PlaceQueens(positions);
        UpdateProgress(message.Value, $"{message.Value} %");
    }

    private void OnSolutionFound(SolutionFoundMessage message)
    {
        var solutionId = ObservableSolutions.Count + 1;
        var newSolution = new Solution(message.Solution, solutionId);

        UpdateSolutionCount();
        AddSolutionToObservable(newSolution);
        SelectedSolution = newSolution;

        UpdateProgress(0, $"Solution {solutionId} found.");
    }

    private void UpdateProgress(double value, string label)
    {
        // Constrain the progress value between 0 and 100
        value = Math.Clamp(value, 0, 100);

        _uiDispatcher.Invoke(() =>
        {
            ProgressValue = value / 100.0;
            ProgressLabel = label;
            OnPropertyChanged(nameof(ProgressLabel));
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

    private void SubscribeToSimulationEvents()
    {
        UnsubscribeFromSimulationEvents();

        Debug.WriteLine("[SubscribeToSimulationEvents] Subscribing to simulation events...");

        // Subscribe to backend events
        if (Solver is BackTrackingSolver backTrackingSolver)
        {
            backTrackingSolver.QueenPlaced += OnQueenPlacedEvent;
            backTrackingSolver.SolutionFound += OnSolutionFoundEvent;
            backTrackingSolver.ProgressValueChanged += OnProgressValueChangedEvent;
        }

        // Register WeakReferenceMessenger handlers
        WeakReferenceMessenger.Default.Register<ProgressValueChangedMessage>(this, (r, m) =>
        {
            Debug.WriteLine("[SubscribeToSimulationEvents] ProgressValueChangedMessage received.");
            OnProgressValueChanged(m);
        });

        WeakReferenceMessenger.Default.Register<QueenPlacedMessage>(this, (r, m) =>
        {
            Debug.WriteLine("[SubscribeToSimulationEvents] QueenPlacedMessage received.");
            OnQueenPlaced(m);
        });

        WeakReferenceMessenger.Default.Register<SolutionFoundMessage>(this, (r, m) =>
        {
            Debug.WriteLine("[SubscribeToSimulationEvents] SolutionFoundMessage received.");
            OnSolutionFound(m);
        });
    }

    private void UnsubscribeFromSimulationEvents()
    {
        Debug.WriteLine("[UnsubscribeFromSimulationEvents] Unsubscribing from simulation events...");

        // Unsubscribe from backend events
        if (Solver is BackTrackingSolver backTrackingSolver)
        {
            backTrackingSolver.QueenPlaced -= OnQueenPlacedEvent;
            backTrackingSolver.SolutionFound -= OnSolutionFoundEvent;
            backTrackingSolver.ProgressValueChanged -= OnProgressValueChangedEvent;
        }

        // Unregister WeakReferenceMessenger handlers
        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
    }

    private void OnSimulationCompleted()
    {
        Debug.WriteLine("[OnSimulationCompleted] Event triggered.");
        UpdateSolutionCount();
        SimulationCompleted?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("[OnSimulationCompleted] Event handling completed.");
    }

    private void OnProgressValueChangedEvent(object? sender, ProgressValueChangedEventArgs e)
    {
        Debug.WriteLine("[OnProgressValueChangedEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    private void OnQueenPlacedEvent(object? sender, QueenPlacedEventArgs e)
    {
        Debug.WriteLine("[OnQueenPlacedEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution, 0));
    }

    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e)
    {
        Debug.WriteLine("[OnSolutionFoundEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));
    }
}
