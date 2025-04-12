namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private InputViewModel InputViewModel { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressLabel))]
    private double _progressValue;

    [ObservableProperty]
    private string _progressLabel;

    [ObservableProperty]
    private Visibility _progressVisibility;

    partial void OnProgressVisibilityChanged(Visibility value)
    {
        IsProgressBarOffscreen = value != Visibility.Visible;
        if (DisplayMode == DisplayMode.Visualize)
        {
            OnPropertyChanged(nameof(ProgressLabel));
        }
    }

    [ObservableProperty]
    private Visibility _progressLabelVisibility;

    partial void OnProgressLabelVisibilityChanged(Visibility value)
    {
        IsProgressLabelOffscreen = value != Visibility.Visible;
    }

    [ObservableProperty]
    private bool _isProgressBarOffscreen;

    [ObservableProperty]
    private bool _isProgressLabelOffscreen;

    [ObservableProperty]
    private IEnumerable<SolutionMode> _enumSolutionModes = Enum.GetValues<SolutionMode>().Cast<SolutionMode>();

    [ObservableProperty]
    private IEnumerable<DisplayMode> _enumDisplayModes = Enum.GetValues<DisplayMode>().Cast<DisplayMode>();

    [ObservableProperty]
    private bool _isVisualized;

    [ObservableProperty]
    private int _delayInMilliseconds;

    partial void OnDelayInMillisecondsChanged(int value)
    {
        Solver.DelayInMilliseconds = value;
    }

    [ObservableProperty]
    private static SimulationResults _simulationResults;

    [ObservableProperty]
    private ObservableCollection<Solution> _observableSolutions = new();

    [ObservableProperty]
    private Solution _selectedSolution;

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            Chessboard.PlaceQueens(value.Positions);
        }
    }

    [ObservableProperty]
    private SolutionMode _solutionMode;

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        if (Solver == null)
        {
            return;
        }

        SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SimulationSettings.MaxNoOfSolutionsInOutput})";

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
        UpdateGui();
    }

    [ObservableProperty]
    private DisplayMode _displayMode;

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        if (Solver == null)
        {
            return;
        }

        IsValid = InputViewModel.Validate(this).IsValid;
        if (!IsValid)
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
        UpdateGui();
    }

    [ObservableProperty]
    private string _boardSizeText;

    partial void OnBoardSizeTextChanged(string value)
    {
        IsValid = InputViewModel.Validate(this).IsValid;

        if (!IsValid)
        {
            IsIdle = false;
            IsSimulating = false;
        }
        else
        {
            IsIdle = true;
            IsSimulating = false;
            IsOutputReady = false;

            // Update the BoardSize property
            BoardSize = int.Parse(value);

            // Notify that BoardSize has changed
            OnPropertyChanged(nameof(BoardSize));

            // Update the UI
            UpdateButtonFunctionality();
            UpdateGui();
        }
    }

    [ObservableProperty]
    private int _boardSize;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _solutionTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultTitle))]
    private string _noOfSolutions;

    [ObservableProperty]
    private string _memoryUsage;

    public string ResultTitle => SolverHelper.SolutionTitle(SolutionMode);

    public Chessboard Chessboard { get; set; }

    public void SetChessboard(double boardDimension)
    {
        BoardSizeText = BoardSize.ToString();
        Chessboard = new Chessboard { WindowWidth = boardDimension, WindowHeight = boardDimension };
        Chessboard.CreateSquares(BoardSize, []);

        IsIdle = true;
        IsSimulating = false;
    }

    [ObservableProperty]
    private string _elapsedTimeInSec;

    [ObservableProperty]
    private bool _isSimulating;

    partial void OnIsSimulatingChanged(bool value)
    {
        UpdateButtonFunctionality();
    }

    [ObservableProperty]
    private bool _isInInputMode;

    partial void OnIsInInputModeChanged(bool value)
    {
        UpdateButtonFunctionality();
    }

    [ObservableProperty]
    private bool _isSingleRunning;

    [ObservableProperty]
    private bool _isIdle;

    partial void OnIsIdleChanged(bool value)
    {
        UpdateButtonFunctionality();
    }

    [ObservableProperty]
    private bool _isOutputReady;

    partial void OnIsOutputReadyChanged(bool value)
    {
        UpdateButtonFunctionality();
    }

    private bool _disposed;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    private readonly ISolver Solver;
}
