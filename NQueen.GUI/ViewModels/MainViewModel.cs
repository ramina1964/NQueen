namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel(ISolver solver)
    {
        _solver = solver
            ?? throw new ArgumentNullException(nameof(solver));

        ObservableSolutions = [];
        Initialize();
        SubscribeToSimulationEvents();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            UnsubscribeFromSimulationEvents();
            ObservableSolutions?.Clear();
        }

        _disposed = true;
    }

    private void Initialize(sbyte boardSize = Utility.DefaultBoardSize,
        SolutionMode solutionMode = Utility.DefaultSolutionMode,
        DisplayMode displayMode = Utility.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };
        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        SaveCommand = new RelayCommand(Save, CanSave);

        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsOutputReady = false;
        NoOfSolutions = $"{ObservableSolutions.Count,0:N0}";

        DelayInMilliseconds = Utility.DefaultDelayInMilliseconds;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;
    }

    private void UpdateGui()
    {
        ObservableSolutions.Clear();
        Chessboard?.Squares.Clear();
        BoardSize = sbyte.Parse(BoardSizeText);
        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0,0:N1}";
        MemoryUsage = "0";
        Chessboard?.CreateSquares(BoardSize, new List<SquareViewModel>());
    }

    private void UpdateButtonFunctionality()
    {
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void ExtractCorrectNoOfSols()
    {
        var sols = SimulationResults
            .Solutions
            .Take(Utility.MaxNoOfSolutionsInOutput);

        if (DisplayMode == DisplayMode.Visualize)
        {
            ObservableSolutions.Clear();
            foreach (var s in sols)
                ObservableSolutions.Add(s);

            return;
        }

        StringBuilder sb = new StringBuilder();
        foreach (var s in sols)
        {
            sb.Append(s.ToString());
            sb.Append(Environment.NewLine);
            ObservableSolutions.Add(s);
        }

        // If you need to use the concatenated string for some purpose
        var concatenatedSolutions = sb.ToString();
    }

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
}