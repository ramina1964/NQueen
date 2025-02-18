namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel)
{
    public void SubscribeToSimulationEvents()
    {
        mainViewModel.Solver.ProgressValueChanged += OnProgressValueChanged;
        mainViewModel.Solver.QueenPlaced += OnQueenPlaced;
        mainViewModel.Solver.SolutionFound += OnSolutionFound;
    }

    public void UnsubscribeFromSimulationEvents()
    {
        mainViewModel.Solver.ProgressValueChanged -= OnProgressValueChanged;
        mainViewModel.Solver.QueenPlaced -= OnQueenPlaced;
        mainViewModel.Solver.SolutionFound -= OnSolutionFound;
    }

    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e)
    {
        mainViewModel.ProgressValue = e.Value;
        mainViewModel.ProgressLabel = $"{e.Value} %";
    }

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution(e.Solution, 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        mainViewModel.Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = mainViewModel.ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        mainViewModel.NoOfSolutions = $"{int.Parse(mainViewModel.NoOfSolutions.Replace(" ", "").Replace(",", "")) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (mainViewModel.ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                mainViewModel.ObservableSolutions.RemoveAt(0);
            }
            if (mainViewModel.ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                mainViewModel.ObservableSolutions.Add(sol);
            }
        }));

        mainViewModel.SelectedSolution = sol;
    }

    // Partial Methods
    #region Partial Methods
    public void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            mainViewModel.Chessboard?.PlaceQueens(value.Positions);

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

    public void OnProgressValueChanged(double value)
    {
        mainViewModel.ProgressLabel = $"{value} %";
    }

    public void OnProgressVisibilityChanged(Visibility value)
    {
        mainViewModel.IsProgressBarOffscreen = value != Visibility.Visible;
    }

    public void OnProgressLabelVisibilityChanged(Visibility value)
    {
        mainViewModel.IsProgressLabelOffscreen = value != Visibility.Visible;
    }

    public void OnDelayInMillisecondsChanged(int value)
    {
        mainViewModel.Solver.DelayInMilliseconds = value;
    }

    public void OnSolutionModeChanged(SolutionMode value)
    {
        if (mainViewModel.Solver == null)
        {
            return;
        }

        mainViewModel.SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SolutionHelper.MaxNoOfSolutionsInOutput})";

        mainViewModel.IsValid = mainViewModel.InputViewModel.Validate(mainViewModel).IsValid;

        if (mainViewModel.IsValid == false)
        {
            mainViewModel.IsIdle = false;
            mainViewModel.IsSimulating = false;
            mainViewModel.IsOutputReady = false;
            mainViewModel.InputViewModel.ErrorMessage = mainViewModel.InputViewModel.Validate(mainViewModel).Errors.FirstOrDefault()?.ErrorMessage;
            mainViewModel.InputViewModel.IsErrorVisible = true;
            mainViewModel.CommandManager.SimulateCommand.NotifyCanExecuteChanged();
            return;
        }

        mainViewModel.IsIdle = true;
        mainViewModel.IsSimulating = false;
        mainViewModel.InputViewModel.IsErrorVisible = false;
        mainViewModel.UpdateGui();
        mainViewModel.CommandManager.SimulateCommand.NotifyCanExecuteChanged();
    }


    public void OnDisplayModeChanged(DisplayMode value)
    {
        mainViewModel.IsValid = mainViewModel.InputViewModel.Validate(mainViewModel).IsValid;

        if (mainViewModel.IsValid)
        {
            mainViewModel.IsIdle = true;
            mainViewModel.IsVisualized = value == DisplayMode.Visualize;
            mainViewModel.UpdateGui();
        }
    }

    public void OnBoardSizeChanged(byte value)
    {
        mainViewModel.IsValid = mainViewModel.InputViewModel.Validate(mainViewModel).IsValid;

        if (mainViewModel.IsValid == false)
        {
            mainViewModel.IsIdle = false;
            mainViewModel.IsSimulating = false;
        }
        else
        {
            mainViewModel.IsIdle = true;
            mainViewModel.IsSimulating = false;
            mainViewModel.IsOutputReady = false;
            mainViewModel.UpdateButtonFunctionality();
            mainViewModel.UpdateGui();
        }
    }

    public void OnNoOfSolutionsChanged(string value) { }

    public void OnIsSimulatingChanged(bool value)
    {
        mainViewModel.UpdateButtonFunctionality();
    }

    public void OnIsInInputModeChanged(bool value)
    {
        mainViewModel.UpdateButtonFunctionality();
    }

    public void OnIsIdleChanged(bool value)
    {
        mainViewModel.UpdateButtonFunctionality();
    }

    public void OnIsOutputReadyChanged(bool value)
    {
        mainViewModel.UpdateButtonFunctionality();
    }

    #endregion Partial Methods
}
