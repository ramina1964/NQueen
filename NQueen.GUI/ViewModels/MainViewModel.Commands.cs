namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    public IAsyncRelayCommand SimulateCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public RelayCommand SaveCommand { get; set; }

    private async Task SimulateAsync()
    {
        ManageSimulationStatus(SimulationStatus.Started);

        UpdateGui();
        SimulationResults = await _solver.GetResultsAsync(BoardSize, SolutionMode, DisplayMode);

        ExtractCorrectNoOfSols();
        NoOfSolutions = $"{SimulationResults.NoOfSolutions,0:N0}";
        ElapsedTimeInSec = $"{SimulationResults.ElapsedTimeInSec,0:N1}";
        SelectedSolution = ObservableSolutions.FirstOrDefault();

        // Update memory usage after the simulation process completes
        MemoryUsage = MemoryMonitoring.UpdateMemoryUsage();

        ManageSimulationStatus(SimulationStatus.Finished);
    }

    private bool CanSimulate() => IsValid && IsIdle;

    private void Cancel() => _solver.IsSolverCanceled = true;

    private bool CanCancel() => IsSimulating;

    private bool CanSave() => IsIdle && IsOutputReady;

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
