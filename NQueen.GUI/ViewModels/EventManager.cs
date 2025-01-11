namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel)
{
    private readonly MainViewModel _mainViewModel = mainViewModel;

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
        //_mainViewModel.NoOfSolutions = $"{int.Parse(_mainViewModel.NoOfSolutions) + 1,0:N0}";
        _mainViewModel.NoOfSolutions = $"{int.Parse(_mainViewModel.NoOfSolutions.Replace(" ", "").Replace(",", "")) + 1,0:N0}";

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

    // Partial Methods
    #region Partial Methods
    public void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            _mainViewModel.Chessboard?.PlaceQueens(value.Positions);

            // Call DisplaySolution on ChessboardUserControl
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainView mainView)
                {
                    var chessboardUserControl = mainView.FindName("ChessboardControl") as ChessboardUserControl;
                    chessboardUserControl?.DisplaySolution(value.Positions);
                }
            });
        }
    }

    public void OnProgressValueChanged(double value) => _mainViewModel.ProgressLabel = $"{value} %";

    public void OnProgressVisibilityChanged(Visibility value)
    {
        _mainViewModel.IsProgressBarOffscreen = value != Visibility.Visible;
    }

    public void OnProgressLabelVisibilityChanged(Visibility value) =>
        _mainViewModel.IsProgressLabelOffscreen = value != Visibility.Visible;

    public void OnDelayInMillisecondsChanged(int value) =>
        _mainViewModel.Solver.DelayInMilliseconds = value;

    public void OnSolutionModeChanged(SolutionMode value)
    {
        if (_mainViewModel.Solver == null)
        {
            return;
        }

        _mainViewModel.SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SolutionHelper.MaxNoOfSolutionsInOutput})";

        _mainViewModel.IsValid = _mainViewModel.InputViewModel.Validate(_mainViewModel).IsValid;

        if (_mainViewModel.IsValid == false)
        {
            _mainViewModel.IsIdle = false;
            _mainViewModel.IsSimulating = false;
            _mainViewModel.IsOutputReady = false;
            return;
        }

        _mainViewModel.IsIdle = true;
        _mainViewModel.IsSimulating = false;
        _mainViewModel.UpdateGui();
    }

    public void OnDisplayModeChanged(DisplayMode value)
    {
        _mainViewModel.IsValid = _mainViewModel.InputViewModel.Validate(_mainViewModel).IsValid;

        if (_mainViewModel.IsValid)
        {
            _mainViewModel.IsIdle = true;
            _mainViewModel.IsVisualized = value == DisplayMode.Visualize;
            _mainViewModel.UpdateGui();
        }
    }

    public void OnBoardSizeChanged(byte value)
    {
        _mainViewModel.IsValid = _mainViewModel.InputViewModel.Validate(_mainViewModel).IsValid;

        if (_mainViewModel.IsValid == false)
        {
            _mainViewModel.IsIdle = false;
            _mainViewModel.IsSimulating = false;
        }
        else
        {
            _mainViewModel.IsIdle = true;
            _mainViewModel.IsSimulating = false;
            _mainViewModel.IsOutputReady = false;
            _mainViewModel.UpdateButtonFunctionality();
            _mainViewModel.UpdateGui();
        }
    }

    public void OnNoOfSolutionsChanged(string value) { }

    public void OnIsSimulatingChanged(bool value) => _mainViewModel.UpdateButtonFunctionality();

    public void OnIsInInputModeChanged(bool value) => _mainViewModel.UpdateButtonFunctionality();

    public void OnIsIdleChanged(bool value) => _mainViewModel.UpdateButtonFunctionality();

    public void OnIsOutputReadyChanged(bool value) => _mainViewModel.UpdateButtonFunctionality();
    #endregion Partial Methods
}
