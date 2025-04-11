namespace NQueen.GUI.ViewModels;

// Todo: Fix a bug where Solution No. in the ListView of the GUI studders, Solution 1, Solution 1, ...
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel() : this(new BackTrackingSolver(new SolutionManager()))
    { }

    public MainViewModel(ISolver solver)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));

        ObservableSolutions = [];
        Initialize();
        SubscribeToSimulationEvents();

        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        SaveCommand = new RelayCommand(Save, CanSave);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            CancelationTokenSource?.Dispose();
            CancelationTokenSource = null;

            // Clear collections
            ObservableSolutions.Clear();
            Chessboard?.Squares.Clear();
        }

        // Dispose unmanaged resources
        _disposed = true;
    }

    private void Initialize(int boardSize = Utility.DefaultBoardSize,
        SolutionMode solutionMode = Utility.DefaultSolutionMode,
        DisplayMode displayMode = Utility.DefaultDisplayMode)
    {
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };

        SimulateCommand = new AsyncRelayCommand(SimulateAsync,
            AsyncRelayCommandOptions.AllowConcurrentExecutions);

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
        BoardSize = int.Parse(BoardSizeText);
        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0,0:N1}";
        MemoryUsage = "0";
        Chessboard?.CreateSquares(BoardSize, []);
    }

    private void UpdateButtonFunctionality()
    {
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void ExtractCorrectNoOfSols()
    {
        // Ensure previous solutions are cleared before adding new ones
        ObservableSolutions.Clear();

        var sols = SimulationResults
            .Solutions
            .Take(Utility.MaxNoOfSolutionsInOutput);

        if (DisplayMode == DisplayMode.Visualize)
        {
            foreach (var s in sols)
                ObservableSolutions.Add(s);

            return;
        }

        StringBuilder sb = new();
        foreach (var s in sols)
        {
            sb.Append(s.ToString());
            sb.Append(Environment.NewLine);
            ObservableSolutions.Add(s);
        }

        // If you need to use the concatenated string for some purpose
        _ = sb.ToString();
    }

    private async Task SimulateAsync()
    {
        ManageSimulationStatus(SimulationStatus.Started);

        UpdateGui();
        SimulationResults = await Solver.GetResultsAsync(BoardSize, SolutionMode, DisplayMode);

        ExtractCorrectNoOfSols();
        NoOfSolutions = $"{SimulationResults.NoOfSolutions,0:N0}";
        ElapsedTimeInSec = $"{SimulationResults.ElapsedTimeInSec,0:N1}";
        SelectedSolution = ObservableSolutions.FirstOrDefault();

        // Update memory usage after the simulation process completes
        MemoryUsage = MemoryMonitoring.UpdateMemoryUsage();

        ManageSimulationStatus(SimulationStatus.Finished);
    }
}
