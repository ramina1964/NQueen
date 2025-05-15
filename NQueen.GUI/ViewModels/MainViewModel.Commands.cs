namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
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
        // Use the save file dialog service to get the file path
        var filePath = _saveFileService.ShowSaveFileDialog();
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.WriteLine("[Save] Save operation canceled by the user.");
            return;
        }

        try
        {
            // Generate the content to save
            var content = GenerateSaveContent();

            // Save the content using the service
            _saveFileService.SaveContent(content);

            Debug.WriteLine($"[Save] Content saved successfully to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Save] Error during save operation: {ex.Message}");
            throw;
        }
    }

    // Todo: Find out why this isn't called when BoardSizeText is wrongly formatted, e.g "abc".
    private bool CanSimulate()
    {
        Debug.WriteLine($"CanSimulate called: IsIdle={IsIdle}, HasErrors={HasErrors}");
        return IsIdle && HasErrors == false;
    }

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

        RefreshCommandStates();
    }

    private string GenerateSaveContent()
    {
        // Generate the content to save (e.g., solutions, settings, etc.)
        StringBuilder sb = new();

        // Set the chessboard size, throw an exception if invalid.
        var boardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);

        sb.AppendLine($"Board Size: {boardSize}");
        sb.AppendLine($"Number of Solutions: {NoOfSolutions}");
        sb.AppendLine($"Elapsed Time: {ElapsedTimeInSec} seconds");
        sb.AppendLine("Solutions:");
        foreach (var solution in ObservableSolutions)
            sb.AppendLine(solution.ToString());

        sb.AppendLine($"Memory Usage: {MemoryUsage} bytes");
        return sb.ToString();
    }
}
