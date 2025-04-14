#nullable enable

namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    /// <summary>
    /// Event triggered when the simulation is completed.
    /// </summary>
    public event EventHandler? SimulationCompleted;

    private void OnProgressValueChanged(ProgressValueChangedMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Debug.WriteLine($"[OnProgressValueChanged] Received ProgressValue: {message.Value}");
            ProgressValue = message.Value;
        });
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        var positions = message.Solution
            .Take(BoardSize)
            .Select((queenPosition, rowIndex) => new Position(rowIndex, queenPosition))
            .ToList();

        Chessboard.PlaceQueens(positions);
    }

    private void OnSolutionFound(SolutionFoundMessage message)
    {
        var solutionId = ObservableSolutions.Count + 1;
        var newSolution = new Solution(message.Solution, solutionId);

        UpdateSolutionCount();
        AddSolutionToObservable(newSolution);
        SelectedSolution = newSolution;
    }

    private void UpdateSolutionCount()
    {
        NoOfSolutions = $"{int.Parse(NoOfSolutions) + 1,0:N0}";
    }

    private void AddSolutionToObservable(Solution solution)
    {
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (ObservableSolutions.Count >= SimulationSettings.MaxNoOfSolutionsInOutput)
            {
                ObservableSolutions.RemoveAt(0);
            }

            if (!ObservableSolutions.Any(existingSolution => existingSolution.Id == solution.Id))
            {
                ObservableSolutions.Add(solution);
            }
        }));
    }

    private void SubscribeToSimulationEvents()
    {
        UnsubscribeFromSimulationEvents();

        Debug.WriteLine("[SubscribeToSimulationEvents] Subscribing to simulation events...");
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
        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
    }

    /// <summary>
    /// Invokes the SimulationCompleted event when the simulation finishes.
    /// </summary>
    private void OnSimulationCompleted()
    {
        SimulationCompleted?.Invoke(this, EventArgs.Empty);
    }
}

#nullable restore