namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private InputViewModel InputViewModel { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressLabel))]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _progressLabel = string.Empty;

    [ObservableProperty]
    private Visibility _progressVisibility;

    partial void OnProgressVisibilityChanged(Visibility value)
    {
        IsProgressBarOffscreen = value != Visibility.Visible;
        if (DisplayMode == DisplayMode.Visualize)
            OnPropertyChanged(nameof(ProgressLabel));
    }

    [ObservableProperty]
    private Visibility _progressLabelVisibility;

    partial void OnProgressLabelVisibilityChanged(Visibility value) =>
        IsProgressLabelOffscreen = value != Visibility.Visible;

    [ObservableProperty]
    private bool _isProgressBarOffscreen;

    [ObservableProperty]
    private bool _isProgressLabelOffscreen;

    [ObservableProperty]
    private IEnumerable<SolutionMode> _enumSolutionModes =
        Enum.GetValues<SolutionMode>().Cast<SolutionMode>();

    [ObservableProperty]
    private IEnumerable<DisplayMode> _enumDisplayModes =
        Enum.GetValues<DisplayMode>().Cast<DisplayMode>();

    [ObservableProperty]
    private bool _isVisualized;

    [ObservableProperty]
    private int _delayInMilliseconds;

    partial void OnDelayInMillisecondsChanged(int value)
    {
        if (Solver != null)
            Solver.DelayInMilliseconds = value;
    }

    [ObservableProperty]
    private static SimulationResults _simulationResults = null!;

    [ObservableProperty]
    private ObservableCollection<Solution> _observableSolutions = [];

    [ObservableProperty]
    private Solution _selectedSolution = new([], null);

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
            ChessboardVm.PlaceQueens(value.Positions);
    }

    [ObservableProperty]
    private SolutionMode _solutionMode;

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        if (Solver == null)
            return;

        SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SimulationSettings.MaxNoOfSolutionsInOutput})";

        // Trigger validation for BoardSizeText
        ValidateProperty(nameof(BoardSizeText));

        OnPropertyChanged(nameof(BoardSizeText));
        OnPropertyChanged(nameof(SolutionTitle));

        IsValid = InputViewModel.Validate(this).IsValid;

        if (IsValid == false)
        {
            IsIdle = false;
            IsSimulating = false;
            IsOutputReady = false;
            return;
        }

        IsIdle = true;
        IsSimulating = false;
        UpdateUiState();
    }

    [ObservableProperty]
    private DisplayMode _displayMode;

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        if (Solver == null)
            return;

        IsValid = InputViewModel.Validate(this).IsValid;
        if (IsValid == false)
        {
            IsIdle = false;
            IsSimulating = false;
            IsOutputReady = false;
            return;
        }

        IsIdle = true;
        IsSimulating = false;
        IsOutputReady = false;
        OnPropertyChanged(nameof(BoardSizeText));
        UpdateUiState();
    }

    [ObservableProperty]
    private string _boardSizeText = string.Empty;

    // A computed property that parses the board size from the text input.
    public int BoardSize => ParsingUtils.ParseIntOrThrow(BoardSizeText);

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private string _solutionTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultTitle))]
    private string _noOfSolutions ="0";

    [ObservableProperty]
    private string _memoryUsage = "0";

    public string ResultTitle => SolverHelper.UpdateSolutionTitle(SolutionMode);

    public ChessboardViewModel ChessboardVm { get; set; }

    public void SetChessboard(double boardDimension)
    {
        // Set the chessboard size, throw an exception if invalid.
        var boardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);

        ChessboardVm.WindowWidth = boardDimension;
        ChessboardVm.WindowHeight = boardDimension;

        // Update the squares with the new board size
        ChessboardVm.CreateSquares(boardSize);

        IsIdle = true;
        IsSimulating = false;
    }

    [ObservableProperty]
    private string _elapsedTimeInSec = string.Empty;

    [ObservableProperty]
    private bool _isSimulating;

    partial void OnIsSimulatingChanged(bool value) =>
        RefreshCommandStates();

    [ObservableProperty]
    private bool _isInInputMode;

    partial void OnIsInInputModeChanged(bool value) =>
        RefreshCommandStates();

    [ObservableProperty]
    private bool _isSingleRunning;

    [ObservableProperty]
    private bool _isIdle;

    partial void OnIsIdleChanged(bool value) =>
        RefreshCommandStates();

    partial void OnIsSingleRunningChanged(bool value) =>
        OnPropertyChanged(nameof(IsSingleRunning));

    [ObservableProperty]
    private bool _isOutputReady;

    partial void OnIsOutputReadyChanged(bool value) =>
        RefreshCommandStates();

    private bool _disposed;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    private readonly ISolver Solver;
}
