namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValue = e.Value;

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution([.. e.Solution], 1);
        var positions = sol
            .QueenList.Where(q => q > -1)
            .Select((item, index) => new Position((sbyte)index, item)).ToList();

        Chessboard.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = ObservableSolutions.Count + 1;
        var sol = new Solution([.. e.Solution], id);

        _ = Application
            .Current
            .Dispatcher
            .BeginInvoke(DispatcherPriority.Send, new Action(() => ObservableSolutions.Add(sol)));

        SelectedSolution = sol;
    }

    private void SubscribeToSimulationEvents()
    {
        _solver.ProgressValueChanged += OnProgressValueChanged;
        _solver.QueenPlaced += OnQueenPlaced;
        _solver.SolutionFound += OnSolutionFound;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        _solver.QueenPlaced -= OnQueenPlaced;
        _solver.SolutionFound -= OnSolutionFound;
        _solver.ProgressValueChanged -= OnProgressValueChanged;
    }
}
