namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel(ISolver solver)
    {
        _solver = solver
            ?? throw new ArgumentNullException(nameof(solver));

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

    private InputViewModel InputViewModel { get; set; }

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
        ObservableSolutions = new ObservableCollection<Solution>();
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
                    .Take(Utility.MaxNoOfSolutionsInOutput)
                    .ToList();

        if (DisplayMode == DisplayMode.Visualize)
        {
            ObservableSolutions.Clear();
            foreach (var s in sols)
            {
                ObservableSolutions.Add(s);
            }
            return;
        }

        foreach (var s in sols)
            ObservableSolutions.Add(s);
    }
}
