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

                Debug.WriteLine($"[ManageSimulationStatus:Started] Initial ProgressVisibility: {ProgressVisibility}");
                Debug.WriteLine($"[ManageSimulationStatus:Started] Initial IsSingleRunning: {IsSingleRunning}");

                if (SolutionMode == SolutionMode.Single)
                {
                    IsSingleRunning = true;
                    ProgressVisibility = Visibility.Visible;
                    ProgressLabelVisibility = Visibility.Hidden;

                    Debug.WriteLine($"[ManageSimulationStatus:Started] Updated for Single Mode - IsSingleRunning: {IsSingleRunning}, ProgressVisibility: {ProgressVisibility}");
                }
                else
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = SolverHelper.StartProgressValue;

                    Debug.WriteLine($"[ManageSimulationStatus:Started] Updated for Other Modes - IsSingleRunning: {IsSingleRunning}, ProgressVisibility: {ProgressVisibility}");
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

                Debug.WriteLine($"[ManageSimulationStatus:Finished] Final ProgressVisibility: {ProgressVisibility}");
                Debug.WriteLine($"[ManageSimulationStatus:Finished] Final IsSingleRunning: {IsSingleRunning}");

                OnSimulationCompleted();
                break;
        }

        UpdateButtonFunctionality();
    }
}
