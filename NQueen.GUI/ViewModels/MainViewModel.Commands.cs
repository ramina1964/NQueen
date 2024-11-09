namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    public IAsyncRelayCommand SimulateCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public RelayCommand SaveCommand { get; set; }

    private bool CanSimulate() => IsIdle && IsValid;

    private void Cancel() => _solver.IsSolverCanceled = true;

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
        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsSingleRunning = false;
        IsOutputReady = true;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;

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
                    IsSingleRunning = true;

                // If SolutionMode.Unique || SolutionMode.All
                else if (IsSimulating)
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = Utility.StartProgressValue;
                }
                break;

            case SimulationStatus.Finished:
                UnsubscribeFromSimulationEvents();
                break;
        }
    }
}
