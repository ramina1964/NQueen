namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    private InputViewModel InputViewModel { get; set; }

    private double _progressValue;

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            SetProperty(ref _progressValue, value);
            ProgressLabel = $"{_progressValue} %";
        }
    }

    private string _progressLabel;

    public string ProgressLabel
    {
        get => _progressLabel;
        set => SetProperty(ref _progressLabel, value);
    }

    private Visibility _progressVisibility;

    public Visibility ProgressVisibility
    {
        get => _progressVisibility;
        set
        {
            if (SetProperty(ref _progressVisibility, value))
            {
                IsProgressBarOffscreen = value != Visibility.Visible;
                if (DisplayMode == DisplayMode.Visualize)
                {
                    OnPropertyChanged(nameof(ProgressLabel));
                }
            }
        }
    }

    private Visibility _progressLabelVisibility;

    public Visibility ProgressLabelVisibility
    {
        get => _progressLabelVisibility;
        set
        {
            if (SetProperty(ref _progressLabelVisibility, value))
            {
                IsProgressLabelOffscreen = value != Visibility.Visible;
            }
        }
    }

    private bool _isProgressBarOffscreen;

    public bool IsProgressBarOffscreen
    {
        get => _isProgressBarOffscreen;
        set => SetProperty(ref _isProgressBarOffscreen, value);
    }

    private bool _isProgressLabelOffscreen;

    public bool IsProgressLabelOffscreen
    {
        get => _isProgressLabelOffscreen;
        set => SetProperty(ref _isProgressLabelOffscreen, value);
    }

    private IEnumerable<SolutionMode> _enumSolutionModes;

    public IEnumerable<SolutionMode> SolutionModeList
    {
        get => Enum.GetValues<SolutionMode>().Cast<SolutionMode>();
        set => SetProperty(ref _enumSolutionModes, value);
    }

    private IEnumerable<DisplayMode> _enumDisplayModes;

    public IEnumerable<DisplayMode> DisplayModeList
    {
        get => Enum.GetValues<DisplayMode>().Cast<DisplayMode>();
        set => SetProperty(ref _enumDisplayModes, value);
    }

    private bool _isVisualized;

    public bool IsVisualized
    {
        get => _isVisualized;
        set => SetProperty(ref _isVisualized, value);
    }

    private int _delayInMilliseconds;
    
    public int DelayInMilliseconds
    {
        get => _delayInMilliseconds;
        set
        {
            SetProperty(ref _delayInMilliseconds, value);
            Solver.DelayInMilliseconds = value;
        }
    }

    private static SimulationResults _simulationResults;

    public SimulationResults SimulationResults
    {
        get => _simulationResults;
        set => SetProperty(ref _simulationResults, value);
    }

    public ObservableCollection<Solution> ObservableSolutions { get; }

    private Solution _selectedSolution;

    public Solution SelectedSolution
    {
        get => _selectedSolution;
        set
        {
            SetProperty(ref _selectedSolution, value);
            if (value != null)
            { Chessboard.PlaceQueens(_selectedSolution.Positions); }
        }
    }

    private SolutionMode _solutionMode;

    public SolutionMode SolutionMode
    {
        get => _solutionMode;
        set
        {
            var isChanged = SetProperty(ref _solutionMode, value);
            if (Solver == null || !isChanged)
            { return; }

            SolutionTitle =
                (SolutionMode == SolutionMode.Single)
                ? $"Solution"
                : $"Solutions (Max: {Utility.MaxNoOfSolutionsInOutput})";

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
    }

    private DisplayMode _displayMode;

    public DisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            _ = SetProperty(ref _displayMode, value);
            IsValid = InputViewModel.Validate(this).IsValid;

            if (IsValid)
            {
                IsIdle = true;
                IsVisualized = value == DisplayMode.Visualize;
                OnPropertyChanged(nameof(BoardSizeText));
                UpdateGui();
            }
        }
    }

    private string _boardSizeText;

    public string BoardSizeText
    {
        get => _boardSizeText;
        set
        {
            if (!SetProperty(ref _boardSizeText, value))
            { return; }
            IsValid = InputViewModel.Validate(this).IsValid;

            if (IsValid == false)
            {
                IsIdle = false;
                IsSimulating = false;
            }

            else
            {
                IsIdle = true;
                IsSimulating = false;
                IsOutputReady = false;
                SetProperty(ref _boardSize, int.Parse(value));
                OnPropertyChanged(nameof(BoardSize));

                UpdateButtonFunctionality();
                UpdateGui();
            }
        }
    }

    private int _boardSize;

    public int BoardSize
    {
        get => _boardSize;
        set => SetProperty(ref _boardSize, value);
    }

    public string ResultTitle => Utility.SolutionTitle(SolutionMode);

    private bool _isValid;

    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    private string _solutionTitle;

    public string SolutionTitle
    {
        get => _solutionTitle;
        set
        {
            if (_solutionTitle != value)
                SetProperty(ref _solutionTitle, value);
        }
    }

    private string _noOfSolutions;

    public string NoOfSolutions
    {
        get => _noOfSolutions;
        set
        {
            if (SetProperty(ref _noOfSolutions, value))
            {
                OnPropertyChanged(nameof(ResultTitle));
            }
        }
    }

    private string _memoryUsage;

    public string MemoryUsage
    {
        get => _memoryUsage;
        set => SetProperty(ref _memoryUsage, value);
    }

    public Chessboard Chessboard { get; set; }

    public void SetChessboard(double boardDimension)
    {
        BoardSizeText = BoardSize.ToString();
        Chessboard = new Chessboard { WindowWidth = boardDimension, WindowHeight = boardDimension };
        Chessboard.CreateSquares(BoardSize, []);

        IsIdle = true;
        IsSimulating = false;
    }

    private string _elapsedTime;

    public string ElapsedTimeInSec
    {
        get => _elapsedTime;
        set => SetProperty(ref _elapsedTime, value);
    }

    private bool _isSimulating;

    public bool IsSimulating
    {
        get => _isSimulating;
        set
        {
            if (SetProperty(ref _isSimulating, value))
            { UpdateButtonFunctionality(); }
        }
    }

    private bool _isInInputMode;

    public bool IsInInputMode
    {
        get => _isInInputMode;
        set
        {
            if (SetProperty(ref _isInInputMode, value))
            { UpdateButtonFunctionality(); }
        }
    }

    private bool _isSingleRunning;

    public bool IsSingleRunning
    {
        get => _isSingleRunning;
        set => SetProperty(ref _isSingleRunning, value);
    }

    private bool _isIdle;

    public bool IsIdle
    {
        get => _isIdle;
        set
        {
            if (SetProperty(ref _isIdle, value))
            { UpdateButtonFunctionality(); }
        }
    }

    private bool _isOutputReady;

    public bool IsOutputReady
    {
        get => _isOutputReady;
        set
        {
            if (SetProperty(ref _isOutputReady, value))
            { UpdateButtonFunctionality(); }
        }
    }

    private bool _disposed;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    private readonly ISolver Solver;
}
