namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValue = e.Value;

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution([.. e.Solution], 1);
        var positions = sol
            .QueenPositions.Where(q => q < Utility.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        Chessboard.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = ObservableSolutions.Count + 1;
        var sol = new Solution([.. e.Solution], id);

        // Update the total number of solutions
        NoOfSolutions = $"{int.Parse(NoOfSolutions) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (ObservableSolutions.Count >= Utility.MaxNoOfSolutionsInOutput)
            {
                ObservableSolutions.RemoveAt(0); // Remove the oldest solution
            }
            ObservableSolutions.Add(sol);
            Debug.WriteLine($"ObservableSolutions count: {ObservableSolutions.Count}");
        }));

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
        _solver.ProgressValueChanged -= OnProgressValueChanged;
        _solver.QueenPlaced -= OnQueenPlaced;
        _solver.SolutionFound -= OnSolutionFound;
    }
}
