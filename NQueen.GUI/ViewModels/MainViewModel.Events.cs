namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void SubscribeToSimulationEvents()
    {
        UnsubscribeFromSimulationEvents();
        if (_solver is SimulationOrchestrator backTrackingSolver)
        {
            backTrackingSolver.QueenPlaced += OnQueenPlacedEvent;
            backTrackingSolver.SolutionFound += OnSolutionFoundEvent;
            backTrackingSolver.ProgressValueChanged += OnProgressValueChangedEvent;
        }

        WeakReferenceMessenger.Default.Register<QueenPlacedMessage>(this, (r, m) =>
            OnQueenPlaced(m));

        WeakReferenceMessenger.Default.Register<SolutionFoundMessage>(this, (r, m) =>
            OnSolutionFound(m));

        WeakReferenceMessenger.Default.Register<ProgressValueChangedMessage>(this, (r, m) =>
            OnProgressValueChanged(m));
    }

    private void UnsubscribeFromSimulationEvents()
    {
        if (_solver is SimulationOrchestrator backTrackingSolver)
        {
            backTrackingSolver.QueenPlaced -= OnQueenPlacedEvent;
            backTrackingSolver.SolutionFound -= OnSolutionFoundEvent;
            backTrackingSolver.ProgressValueChanged -= OnProgressValueChangedEvent;
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

    private void OnProgressValueChanged(ProgressValueChangedMessage message) =>
        UpdateProgress(message.Value, $"{Math.Round(message.Value * 100, 1)} %");

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
        UpdateProgress(message.Value, $"{Math.Round(message.Value * 100, 1)} %");
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
        value = Math.Clamp(value, 0, 1);
        _uiDispatcher.Invoke(() =>
        {
            if (IsSingleRunning == false)
            {
                ProgressValue = value;
                ProgressLabel = label;
            }

            // Always show percent in label, regardless of input label
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

    private void OnProgressValueChangedEvent(object? sender,
        ProgressValueChangedWithTokenEventArgs e)
    {
        if (e.SimulationToken != _currentSimulationToken)
            return;

        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    private void OnQueenPlacedEvent(object? sender, QueenPlacedEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution, 0));

    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e) =>
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));
}
