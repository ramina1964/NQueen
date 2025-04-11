namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void OnProgressValueChanged(ProgressValueChangedMessage message)
    {
        ProgressValue = message.Value;
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
            if (ObservableSolutions.Count >= Utility.MaxNoOfSolutionsInOutput)
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

        WeakReferenceMessenger.Default.Register<ProgressValueChangedMessage>(this, (r, m) =>
            OnProgressValueChanged(m));

        WeakReferenceMessenger.Default.Register<QueenPlacedMessage>(this, (r, m) =>
            OnQueenPlaced(m));

        WeakReferenceMessenger.Default.Register<SolutionFoundMessage>(this, (r, m) =>
            OnSolutionFound(m));
    }

    private void UnsubscribeFromSimulationEvents()
    {
        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
    }
}
