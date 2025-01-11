namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable, IDataErrorInfo
{
    // Constructors
    #region Constructors

    public MainViewModel(ISolver solver, ICommandManager commandManager)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));
        CommandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        CommandManager.Initialize(this);
        ObservableSolutions = [];

        _eventManager = new EventManager(this);
        Initialize();
        _eventManager.SubscribeToSimulationEvents();
    }

    #endregion Constructors

    public ICommandManager CommandManager
    {
        get => _commandManager;
        set
        {
            _commandManager = value;
            _commandManager?.Initialize(this);
        }
    }

    // IDisposable Implementation
    #region IDisposable Implementation
    private bool _disposed;

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
    #endregion IDisposable Implementation

    // IDataErrorInfo Implementation
    #region IDataErrorInfo Implementation
    public string this[string columnName]
    {
        get
        {
            var validationFailure = InputViewModel
                .Validate(this)
                .Errors
                .FirstOrDefault(item => item.PropertyName == columnName);

            return validationFailure == null
                   ? string.Empty
                   : validationFailure.ErrorMessage;
        }
    }

    public string Error
    {
        get
        {
            var results = InputViewModel.Validate(this);
            if (results == null || results.Errors.Count == 0)
            { return string.Empty; }

            var errors = string
                .Join(Environment.NewLine, results.Errors
                .Select(x => x.ErrorMessage)
                .ToArray());

            return errors;
        }
    }
    #endregion IDataErrorInfo Implementation

    // Observable Properties
    #region Observable Properties
    [ObservableProperty]
    private InputViewModel _inputViewModel;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _progressLabel;

    [ObservableProperty]
    private Visibility _progressVisibility;

    [ObservableProperty]
    private Visibility _progressLabelVisibility;

    [ObservableProperty]
    private bool _isProgressBarOffscreen;

    [ObservableProperty]
    private bool _isProgressLabelOffscreen;

    [ObservableProperty]
    private IEnumerable<SolutionMode> _solutionModeList =
        Enum.GetValues<SolutionMode>().Cast<SolutionMode>();

    [ObservableProperty]
    private IEnumerable<DisplayMode> _displayModeList =
        Enum.GetValues<DisplayMode>().Cast<DisplayMode>();

    [ObservableProperty]
    private bool _isVisualized;

    [ObservableProperty]
    private int _delayInMilliseconds;

    [ObservableProperty]
    private static SimulationResults _simulationResults;

    [ObservableProperty]
    public ObservableCollection<Solution> _observableSolutions;

    [ObservableProperty]
    private Solution _selectedSolution;

    [ObservableProperty]
    private SolutionMode _solutionMode;

    [ObservableProperty]
    private DisplayMode _displayMode;

    [ObservableProperty]
    private byte _boardSize;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _solutionTitle;

    [ObservableProperty]
    private string _noOfSolutions;

    [ObservableProperty]
    private string _memoryUsage;

    [ObservableProperty]
    public ChessboardViewModel _chessboard;

    [ObservableProperty]
    private string _elapsedTimeInSec;

    [ObservableProperty]
    private bool _isSimulating;

    [ObservableProperty]
    private bool _isInInputMode;

    [ObservableProperty]
    private bool _isSingleRunning;

    [ObservableProperty]
    private bool _isIdle;

    [ObservableProperty]
    private bool _isOutputReady;

    [ObservableProperty]
    private bool _hasValidationError;

    [ObservableProperty]
    private string _validationError;
    #endregion Observable Properties

    // Other properties and fields
    public string ResultTitle => SolutionHelper.SolutionTitle(SolutionMode);

    public readonly ISolver Solver;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    // Methods
    #region Methods
    private void Initialize(byte boardSize = BoardSettings.DefaultBoardSize,
        SolutionMode solutionMode = SolutionHelper.DefaultSolutionMode,
        DisplayMode displayMode = SolutionHelper.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };

        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsOutputReady = false;
        NoOfSolutions = $"{ObservableSolutions.Count,0:N0}";

        DelayInMilliseconds = SolutionHelper.DefaultDelayInMilliseconds;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;
    }

    public void UpdateGui()
    {
        ObservableSolutions.Clear();
        Chessboard?.Squares.Clear();
        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0}";
        MemoryUsage = "0";
        Chessboard?.CreateSquares(BoardSize, []);
    }

    public void UpdateButtonFunctionality()
    {
        if (CommandManager == null)
            return;

        CommandManager.SimulateCommand?.NotifyCanExecuteChanged();
        CommandManager.CancelCommand?.NotifyCanExecuteChanged();
        CommandManager.SaveCommand?.NotifyCanExecuteChanged();
    }

    public void ExtractCorrectNoOfSols()
    {
        // Ensure previous solutions are cleared before adding new ones
        ObservableSolutions.Clear();

        var sols = SimulationResults
            .Solutions
            .Take(SolutionHelper.MaxNoOfSolutionsInOutput);

        foreach (var s in sols)
            ObservableSolutions.Add(s);
    }

    public void ManageSimulationStatus(SimulationStatus simulationStatus)
    {
        switch (simulationStatus)
        {
            case SimulationStatus.Started:
                _eventManager.SubscribeToSimulationEvents();

                IsIdle = false;
                IsInInputMode = false;
                IsSimulating = true;
                IsOutputReady = false;

                ProgressVisibility = Visibility.Visible;
                if (SolutionMode == SolutionMode.Single)
                {
                    IsSingleRunning = true;
                }
                else
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = ProgressSettings.StartProgressValue;
                }
                break;

            case SimulationStatus.Finished:
                _eventManager.UnsubscribeFromSimulationEvents();

                IsIdle = true;
                IsInInputMode = true;
                IsSimulating = false;
                IsSingleRunning = false;
                IsOutputReady = true;
                ProgressVisibility = Visibility.Hidden;
                ProgressLabelVisibility = Visibility.Hidden;
                break;
        }

        // Notify the commands to re-evaluate their CanExecute state
        CommandManager.SimulateCommand.NotifyCanExecuteChanged();
        CommandManager.CancelCommand.NotifyCanExecuteChanged();
        CommandManager.SaveCommand.NotifyCanExecuteChanged();
    }

    public void SetChessboard(double boardDimension)
    {
        Chessboard = new ChessboardViewModel
        {
            WindowWidth = boardDimension,
            WindowHeight = boardDimension
        };

        Chessboard.CreateSquares(BoardSize, []);

        IsIdle = true;
        IsSimulating = false;
    }

    partial void OnBoardSizeChanged(byte value)
    {
        // Validate the new board size
        var validationResult = InputViewModel.Validate(this);

        if (!validationResult.IsValid)
        {
            // Handle validation failure (e.g., display an error message)
            ValidationError = validationResult.Errors.First().ErrorMessage;
            HasValidationError = true;
            return;
        }

        // Clear validation error if validation passes
        ValidationError = string.Empty;
        HasValidationError = false;

        // If validation passes, proceed with initialization and GUI update
        Initialize(value, SolutionMode, DisplayMode);
        UpdateGui();
    }

    partial void OnSolutionModeChanged(SolutionMode oldValue, SolutionMode newValue)
    {
        Initialize(BoardSize, newValue, DisplayMode);
        UpdateGui();
    }

    partial void OnDisplayModeChanged(DisplayMode oldValue, DisplayMode newValue)
    {
        Validate();
    }

    partial void OnSelectedSolutionChanged(Solution oldValue, Solution newValue)
    {
        if (newValue != null)
        {
            Chessboard?.PlaceQueens(newValue.Positions);
        }
    }

    private void Validate()
    {
        IsValid = InputViewModel.Validate(this).IsValid;
        UpdateButtonFunctionality();
    }
    #endregion Methods

    private ICommandManager _commandManager;
    private readonly EventManager _eventManager;
}
