namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private void Simulate()
    {
        // Start the simulation asynchronously
        _ = SimulateAsync();
    }

    private void Cancel()
    {
        if (IsSimulating)
        {
            CancelationTokenSource?.Cancel();
            Solver.IsSolverCanceled = true;
            IsSimulating = false;
        }
    }

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
                {
                    IsSingleRunning = true; // Set indeterminate state
                    ProgressValue = 0; // Ensure no value is displayed
                    ProgressLabelVisibility = Visibility.Hidden; // Hide the label
                }
                else
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = SolverHelper.StartProgressValue;
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

                OnSimulationCompleted();
                break;
        }

        UpdateButtonFunctionality();
    }
}
