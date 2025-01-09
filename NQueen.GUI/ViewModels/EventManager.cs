namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel)
{
    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        _mainViewModel.ProgressValue = e.Value;

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution(e.Solution, 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        _mainViewModel.Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = _mainViewModel.ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        _mainViewModel.NoOfSolutions = $"{int.Parse(_mainViewModel.NoOfSolutions) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (_mainViewModel.ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                _mainViewModel.ObservableSolutions.RemoveAt(0);
            }
            if (_mainViewModel.ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                _mainViewModel.ObservableSolutions.Add(sol);
            }
        }));

        _mainViewModel.SelectedSolution = sol;
    }

    public void SubscribeToSimulationEvents()
    {
        _mainViewModel.Solver.ProgressValueChanged += OnProgressValueChanged;
        _mainViewModel.Solver.QueenPlaced += OnQueenPlaced;
        _mainViewModel.Solver.SolutionFound += OnSolutionFound;
    }

    public void UnsubscribeFromSimulationEvents()
    {
        _mainViewModel.Solver.ProgressValueChanged -= OnProgressValueChanged;
        _mainViewModel.Solver.QueenPlaced -= OnQueenPlaced;
        _mainViewModel.Solver.SolutionFound -= OnSolutionFound;
    }

    private readonly MainViewModel _mainViewModel = mainViewModel;
}