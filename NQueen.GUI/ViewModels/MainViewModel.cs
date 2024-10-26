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

    // Dispose of resources held by MainViewModel, e.g. unsubscribing from events, clearing collections.
    public void Dispose()
    {
        // Unsubscribe from events
        UnsubscribeFromSimulationEvents();

        // Clear collections
        ObservableSolutions?.Clear();
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
        ObservableSolutions = [];
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
        var sols = SimulationResults
                    .Solutions
                    .Take(Utility.MaxNoOfSolutionsInOutput)
                    .ToList();

        // In case of activated visualization, clear all solutions before adding a no. of MaxNoOfSolutionsInOutput to the solutions.
        if (DisplayMode == DisplayMode.Visualize)
        {
            ObservableSolutions.Clear();
            sols.ForEach(s => ObservableSolutions.Add(s));
            return;
        }
        sols.ForEach(s => ObservableSolutions.Add(s));
    }
}
