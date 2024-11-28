namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private InputViewModel _inputViewModel;

    [ObservableProperty]
    private double _progressValue;

    partial void OnProgressValueChanged(double value)
    {
        ProgressLabel = $"{value} %";
    }

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
    private IEnumerable<SolutionMode> _solutionModeList = Enum.GetValues<SolutionMode>().Cast<SolutionMode>();

    [ObservableProperty]
    private IEnumerable<DisplayMode> _displayModeList = Enum.GetValues<DisplayMode>().Cast<DisplayMode>();

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
    public ObservableCollection<Solution> _observableSolutions = new();

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
            : $"Solutions (Max: {Utility.MaxNoOfSolutionsInOutput})";

        OnPropertyChanged(nameof(BoardSize));
        OnPropertyChanged(nameof(SolutionTitle));
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
        UpdateGui();
    }

    [ObservableProperty]
    private DisplayMode _displayMode;

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        IsValid = InputViewModel.Validate(this).IsValid;

        if (IsValid)
        {
            IsIdle = true;
            IsVisualized = value == DisplayMode.Visualize;
            OnPropertyChanged(nameof(BoardSize));
            UpdateGui();
        }
    }

    [ObservableProperty]
    private byte _boardSize;

    partial void OnBoardSizeChanged(byte value)
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
            UpdateButtonFunctionality();
            UpdateGui();
        }
    }

    public string ResultTitle => Utility.SolutionTitle(SolutionMode);

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _solutionTitle;

    [ObservableProperty]
    private string _noOfSolutions;

    partial void OnNoOfSolutionsChanged(string value)
    {
        OnPropertyChanged(nameof(ResultTitle));
    }

    [ObservableProperty]
    private string _memoryUsage;

    [ObservableProperty]
    public ChessboardViewModel _chessboard;

    public void SetChessboard(double boardDimension)
    {
        Chessboard = new ChessboardViewModel { WindowWidth = boardDimension, WindowHeight = boardDimension };
        Chessboard.CreateSquares(BoardSize, new List<SquareViewModel>());

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
