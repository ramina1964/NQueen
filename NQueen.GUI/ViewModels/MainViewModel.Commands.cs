namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void Simulate()
    {
        // Start the simulation asynchronously
        _ = SimulateAsync();
    }

    private void Cancel() => Solver.IsSolverCanceled = true;

    private void Save()
    {
        var results = new ResultPresentation(SimulationResults);
        var filePath = results.Write2File(SolutionMode);
        var msg = $"Successfully wrote results to: {filePath}";
        MessageBox.Show(msg);
        IsIdle = true;
    }

    private bool CanSimulate() => IsIdle && IsValid;

    private bool CanCancel() => IsSimulating;

    private bool CanSave() => IsOutputReady;

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
}
