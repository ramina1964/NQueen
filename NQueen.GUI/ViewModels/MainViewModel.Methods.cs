namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    public IAsyncRelayCommand SimulateCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public RelayCommand SaveCommand { get; set; }

    private bool CanSimulate() => IsIdle && IsValid;

    private void Cancel() => Solver.IsSolverCanceled = true;

    private bool CanCancel() => IsSimulating;

    private bool CanSave() => IsOutputReady;

    private void Save()
    {
        var results = new ResultPresentation(SimulationResults);
        var filePath = results.Write2File(SolutionMode);
        var msg = $"Successfully wrote results to: {filePath}";
        MessageBox.Show(msg);
        IsIdle = true;
    }

    private void ManageSimulationStatus(SimulationStatus simulationStatus)
    {
        switch (simulationStatus)
        {
            case SimulationStatus.Started:
                SubscribeToSimulationEvents();

                IsIdle = false;
                IsInInputMode = false;
                IsSimulating = true;
                IsOutputReady = false;

                ProgressVisibility = Visibility.Visible;
                if (SolutionMode == SolutionMode.Single)
                {
                    IsSingleRunning = true;
                }
                else
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = ProgressSettings.StartProgressValue;
                }
                break;

            case SimulationStatus.Finished:
                UnsubscribeFromSimulationEvents();

                IsIdle = true;
                IsInInputMode = true;
                IsSimulating = false;
                IsSingleRunning = false;
                IsOutputReady = true;
                ProgressVisibility = Visibility.Hidden;
                ProgressLabelVisibility = Visibility.Hidden;
                break;
        }

        // Notify the commands to re-evaluate their CanExecute state
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnProgressValueChanged(double value) => ProgressLabel = $"{value} %";

    partial void OnProgressVisibilityChanged(Visibility value)
    {
        IsProgressBarOffscreen = value != Visibility.Visible;
        if (DisplayMode == DisplayMode.Visualize)
        {
            OnPropertyChanged(nameof(ProgressLabel));
        }
    }

    partial void OnProgressLabelVisibilityChanged(Visibility value) =>
        IsProgressLabelOffscreen = value != Visibility.Visible;

    partial void OnDelayInMillisecondsChanged(int value) =>
        Solver.DelayInMilliseconds = value;

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            Chessboard.PlaceQueens(value.Positions);
        }
    }

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        if (Solver == null)
        {
            return;
        }

        SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SolutionHelper.MaxNoOfSolutionsInOutput})";

        OnPropertyChanged(nameof(BoardSize));
        OnPropertyChanged(nameof(SolutionTitle));
        IsValid = InputViewModel.Validate(this).IsValid;

        if (IsValid == false)
        {
            IsIdle = false;
            IsSimulating = false;
            IsOutputReady = false;
            return;
        }

        IsIdle = true;
        IsSimulating = false;
        UpdateGui();
    }

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        IsValid = InputViewModel.Validate(this).IsValid;

        if (IsValid)
        {
            IsIdle = true;
            IsVisualized = value == DisplayMode.Visualize;
            OnPropertyChanged(nameof(BoardSize));
            UpdateGui();
        }
    }

    partial void OnBoardSizeChanged(int value)
    {
        IsValid = InputViewModel.Validate(this).IsValid;

        if (IsValid == false)
        {
            IsIdle = false;
            IsSimulating = false;
        }
        else
        {
            IsIdle = true;
            IsSimulating = false;
            IsOutputReady = false;
            UpdateButtonFunctionality();
            UpdateGui();
        }
    }

    partial void OnNoOfSolutionsChanged(string value) =>
        OnPropertyChanged(nameof(ResultTitle));

    partial void OnIsSimulatingChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsInInputModeChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsIdleChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsOutputReadyChanged(bool value) => UpdateButtonFunctionality();
}
