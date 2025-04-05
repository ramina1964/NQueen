namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable, IDataErrorInfo
{
    public MainViewModel(ISolver solver, ICommandManager commandManager, InputValidator validator)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        CommandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        CommandManager.Initialize(this);
        ObservableSolutions = [];

        _eventManager = new EventManager(this);
        Initialize();
        _eventManager.SubscribeToSimulationEvents();
    }

    public ICommandManager CommandManager
    {
        get => _commandManager;
        set
        {
            _commandManager = value;
            _commandManager?.Initialize(this);
        }
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
    private int _boardSize;

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

    [ObservableProperty]
    private bool _isSimulateButtonEnabled;

    public string ResultTitle => SolutionHelper.SolutionTitle(SolutionMode);

    public readonly ISolver Solver;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    public void Initialize(int boardSize = BoardSettings.DefaultBoardSize,
        SolutionMode solutionMode = SolutionHelper.DefaultSolutionMode,
        DisplayMode displayMode = SolutionHelper.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel(_validator);

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
        // Validate board size before updating the GUI
        if (IsBoardSizeValid(BoardSize, SolutionMode))
        {
            ObservableSolutions.Clear();
            Chessboard?.Squares.Clear();
            NoOfSolutions = "0";
            ElapsedTimeInSec = $"{0}";
            MemoryUsage = "0";

            // Assuming the available width and height are equal to the window dimensions
            Chessboard?.CreateSquares(BoardSize, [], Chessboard.WindowWidth, Chessboard.WindowHeight);
        }
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
        if (IsBoardSizeValid(BoardSize, SolutionMode))
        {
            Chessboard = new ChessboardViewModel
            {
                WindowWidth = boardDimension,
                WindowHeight = boardDimension
            };

            Chessboard.CreateSquares(BoardSize, [], boardDimension, boardDimension);

            IsIdle = true;
            IsSimulating = false;
        }
    }

    partial void OnBoardSizeChanged(int value)
    {
        Validate();
        if (IsValid)
        {
            _eventManager.OnBoardSizeChanged();
        }
    }

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        Validate();
        if (IsValid)
        {
            _eventManager.OnSolutionModeChanged(value);
        }
    }

    partial void OnDisplayModeChanged(DisplayMode oldValue, DisplayMode newValue) =>
        _eventManager.OnDisplayModeChanged();

    partial void OnSelectedSolutionChanged(Solution oldValue, Solution newValue) =>
        Chessboard?.PlaceQueens(newValue?.Positions);

    private void Validate()
    {
        var validationResult = InputViewModel.Validate(Solver, CommandManager, this);
        IsValid = validationResult.IsValid;
        IsSimulateButtonEnabled = validationResult.IsValid;
        UpdateButtonFunctionality();
    }

    public string this[string columnName]
    {
        get
        {
            var validationFailure = InputViewModel
                .Validate(Solver, CommandManager, this) // Pass 'this' as the third argument
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
            var results = InputViewModel.Validate(Solver, CommandManager, this); // Pass 'this' as the third argument
            if (results == null || results.Errors.Count == 0)
            { return string.Empty; }

            var errors = string
                .Join(Environment.NewLine, results.Errors
                .Select(x => x.ErrorMessage)
                .ToArray());

            return errors;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is MainViewModel model &&
               EqualityComparer<InputViewModel>.Default.Equals(InputViewModel, model.InputViewModel);
    }

    public override int GetHashCode() =>
        HashCode.Combine(InputViewModel);

    private static bool IsBoardSizeValid(int boardSize, SolutionMode solutionMode)
    {
        return boardSize >= BoardSettings.MinBoardSize &&
               boardSize <= BoardSettings.MaxBoardSize &&
               (solutionMode != SolutionMode.Single || boardSize <= BoardSettings.MaxBoardSizeInSingleSolution) &&
               (solutionMode != SolutionMode.Unique || boardSize <= BoardSettings.MaxBoardSizeInUniqueSolutions) &&
               (solutionMode != SolutionMode.All || boardSize <= BoardSettings.MaxBoardSizeInAllSolutions);
    }

    private EventManager _eventManager;
    private ICommandManager _commandManager;
    private InputValidator _validator;
    private bool _disposed;
}
