namespace NQueen.GUI.ViewModels;

// Todo: Queens must be placed from columnwise from buttom and upward
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel(IDispatcher uiDispatcher) : this(new BackTrackingSolver(
        new SolutionManager()), uiDispatcher, new SaveFileDialogService())
    { }

    public MainViewModel(
        ISolver solver, IDispatcher dispatcher, ISaveFileDialogService saveFileService)
    {
        Solver = solver
            ?? throw new ArgumentNullException(nameof(solver));

        _uiDispatcher = dispatcher
            ?? throw new ArgumentNullException(nameof(dispatcher));

        _saveFileService = saveFileService
            ?? throw new ArgumentNullException(nameof(saveFileService));

        // Initialize non-nullable properties
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };
        CancelationTokenSource = new CancellationTokenSource();
        ChessboardVm = new ChessboardViewModel(_uiDispatcher);

        // Initialize commands directly
        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel, CanCancel);

        Initialize();
        SubscribeToSimulationEvents();
    }

    public IAsyncRelayCommand SimulateCommand { get; private set; }

    public IRelayCommand SaveCommand { get; private set; }

    public IRelayCommand CancelCommand { get; private set; }

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
            CancelationTokenSource = null!;

            // Clear collections
            ObservableSolutions.Clear();
            ChessboardVm?.Squares.Clear();
        }

        // Dispose unmanaged resources
        _disposed = true;
    }

    private void Initialize(int boardSize = BoardSettings.DefaultBoardSize,
        SolutionMode solutionMode = SimulationSettings.DefaultSolutionMode,
        DisplayMode displayMode = SimulationSettings.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };
        BoardSizeText = boardSize.ToString();
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsOutputReady = false;
        NoOfSolutions = $"{ObservableSolutions.Count,0:N0}";

        DelayInMilliseconds = SimulationSettings.DefaultDelayInMilliseconds;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;
    }

    private void UpdateUiState()
    {
        ObservableSolutions.Clear();
        ChessboardVm?.Squares.Clear();

        // Set the chessboard size, throw an exception if invalid.
        var boardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);

        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0,0:N1}";
        MemoryUsage = "0";
        ChessboardVm?.CreateSquares(boardSize);
    }

    private void RefreshCommandStates()
    {
        SimulateCommand?.NotifyCanExecuteChanged();
        CancelCommand?.NotifyCanExecuteChanged();
        SaveCommand?.NotifyCanExecuteChanged();
    }

    private void ExtractCorrectNoOfSols()
    {
        // Ensure previous solutions are cleared before adding new ones
        Debug.WriteLine("[ExtractCorrectNoOfSols] Clearing previous solutions.");
        ObservableSolutions.Clear();

        var sols = SimulationResults
            .Solutions
            .Take(SimulationSettings.MaxNoOfSolutionsInOutput);

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

        _ = sb.ToString();
    }

    private async Task SimulateAsync()
    {
        try
        {
            Debug.WriteLine("[SimulateAsync] Starting simulation...");
            ManageSimulationStatus(SimulationStatus.Started);

            // Set the chessboard size, throw an exception if invalid.
            var boardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);

            UpdateUiState();
            SimulationResults =
                await Solver.GetResultsAsync(boardSize, SolutionMode, DisplayMode);

            if (SimulationResults == null || !SimulationResults.Solutions.Any())
            {
                Debug.WriteLine("[SimulateAsync] No solutions generated.");
                throw new InvalidOperationException("No solutions were generated by the solver.");
            }

            ExtractCorrectNoOfSols();
            NoOfSolutions = $"{SimulationResults.NoOfSolutions,0:N0}";
            ElapsedTimeInSec = $"{SimulationResults.ElapsedTimeInSec,0:N1}";
            SelectedSolution = ObservableSolutions.FirstOrDefault() ?? new Solution([], null);
            MemoryUsage = NumericUtility.UpdateMemoryUsage();

            Debug.WriteLine("[SimulateAsync] Simulation completed successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SimulateAsync] Error: {ex.Message}");
            MessageBox.Show($"An error occurred during simulation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ManageSimulationStatus(SimulationStatus.Finished);
            Debug.WriteLine("[SimulateAsync] Simulation status set to Finished.");
        }
    }

    private readonly IDispatcher _uiDispatcher;
    private readonly ISaveFileDialogService _saveFileService;
}
