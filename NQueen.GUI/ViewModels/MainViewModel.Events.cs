#nullable enable

using NQueen.Kernel.Events;

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

            // Correct the scaling if ProgressValue is in the range 0-100
            var scaledValue = message.Value > 1 ? message.Value / 100 : message.Value;

            // Update the progress label to show the percentage of work completed
            ProgressLabel = $"{scaledValue:P1}";
            OnPropertyChanged(nameof(ProgressLabel));
        });
    }

    private void OnQueenPlaced(QueenPlacedMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Debug.WriteLine("[OnQueenPlaced] Received QueenPlacedMessage.");
            var positions = message.Solution
                .Take(BoardSize)
                .Select((queenPosition, rowIndex) => new Position(rowIndex, queenPosition))
                .ToList();

            Chessboard.PlaceQueens(positions);

            // Update the progress label
            ProgressLabel = $"Queen placed on row {positions.Count}.";
            OnPropertyChanged(nameof(ProgressLabel)); // Notify the UI about the change
        });
    }

    private void OnSolutionFound(SolutionFoundMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Debug.WriteLine("[OnSolutionFound] Received SolutionFoundMessage.");
            var solutionId = ObservableSolutions.Count + 1;
            var newSolution = new Solution(message.Solution, solutionId);

            UpdateSolutionCount();
            AddSolutionToObservable(newSolution);
            SelectedSolution = newSolution;

            // Update the progress label
            ProgressLabel = $"Solution {solutionId} found.";
            OnPropertyChanged(nameof(ProgressLabel)); // Notify the UI about the change
        });
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

    /// <summary>
    /// Invokes the SimulationCompleted event when the simulation finishes.
    /// </summary>
    private void OnSimulationCompleted()
    {
        SimulationCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnProgressValueChangedEvent(object? sender, ProgressValueChangedEventArgs e)
    {
        Debug.WriteLine("[OnProgressValueChangedEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new ProgressValueChangedMessage(e.Value));
    }

    private void OnQueenPlacedEvent(object? sender, QueenPlacedEventArgs e)
    {
        Debug.WriteLine("[OnQueenPlacedEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new QueenPlacedMessage(e.Solution));
    }

    private void OnSolutionFoundEvent(object? sender, SolutionFoundEventArgs e)
    {
        Debug.WriteLine("[OnSolutionFoundEvent] Backend event received.");
        WeakReferenceMessenger.Default.Send(new SolutionFoundMessage(e.Solution));
    }
}

#nullable restore