namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void HandleProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValue = e.Value;

    private void HandleQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution([.. e.Solution], 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        Chessboard.PlaceQueens(positions);
    }

    private void HandleSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        NoOfSolutions = $"{int.Parse(NoOfSolutions) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                ObservableSolutions.RemoveAt(0);
            }
            if (ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                ObservableSolutions.Add(sol);
            }
        }));

        SelectedSolution = sol;
    }

    private void SubscribeToSimulationEvents()
    {
        Solver.ProgressValueChanged += HandleProgressValueChanged;
        Solver.QueenPlaced += HandleQueenPlaced;
        Solver.SolutionFound += HandleSolutionFound;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        Solver.ProgressValueChanged -= HandleProgressValueChanged;
        Solver.QueenPlaced -= HandleQueenPlaced;
        Solver.SolutionFound -= HandleSolutionFound;
    }
}
