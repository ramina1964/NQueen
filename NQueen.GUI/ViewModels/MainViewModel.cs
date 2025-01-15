namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable, IDataErrorInfo
{

    public MainViewModel(ISolver solver, ICommandManager commandManager)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));
        CommandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        CommandManager.Initialize(this);
        ObservableSolutions = new ObservableCollection<Solution>();

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

    [ObservableProperty]
    private InputViewModel inputViewModel = new InputViewModel();

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string progressLabel;

    [ObservableProperty]
    private Visibility progressVisibility;

    [ObservableProperty]
    private Visibility progressLabelVisibility;

    [ObservableProperty]
    private bool isProgressBarOffscreen;

    [ObservableProperty]
    private bool isProgressLabelOffscreen;

    [ObservableProperty]
    private IEnumerable<SolutionMode> solutionModeList =
        Enum.GetValues<SolutionMode>().Cast<SolutionMode>();

    [ObservableProperty]
    private IEnumerable<DisplayMode> displayModeList =
        Enum.GetValues<DisplayMode>().Cast<DisplayMode>();

    [ObservableProperty]
    private bool isVisualized;

    [ObservableProperty]
    private int delayInMilliseconds;

    [ObservableProperty]
    private static SimulationResults simulationResults;

    [ObservableProperty]
    public ObservableCollection<Solution> observableSolutions;

    [ObservableProperty]
    private Solution selectedSolution;

    [ObservableProperty]
    private SolutionMode solutionMode;

    [ObservableProperty]
    private DisplayMode displayMode;

    [ObservableProperty]
    private byte boardSize;

    [ObservableProperty]
    private bool isValid;

    [ObservableProperty]
    private string solutionTitle;

    [ObservableProperty]
    private string noOfSolutions;

    [ObservableProperty]
    private string memoryUsage;

    [ObservableProperty]
    public ChessboardViewModel chessboard;

    [ObservableProperty]
    private string elapsedTimeInSec;

    [ObservableProperty]
    private bool isSimulating;

    [ObservableProperty]
    private bool isInInputMode;

    [ObservableProperty]
    private bool isSingleRunning;

    [ObservableProperty]
    private bool isIdle;

    [ObservableProperty]
    private bool isOutputReady;

    [ObservableProperty]
    private bool hasValidationError;

    [ObservableProperty]
    private string validationError;

    [ObservableProperty]
    private bool isInputValid;

    public string ResultTitle => SolutionHelper.SolutionTitle(SolutionMode);

    public readonly ISolver Solver;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    private void Initialize(byte boardSize = BoardSettings.DefaultBoardSize,
        SolutionMode solutionMode = SolutionHelper.DefaultSolutionMode,
        DisplayMode displayMode = SolutionHelper.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel();

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
        Chessboard?.CreateSquares(BoardSize, new List<SquareViewModel>());
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

        Chessboard.CreateSquares(BoardSize, new List<SquareViewModel>());

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
            IsValid = false;
            IsInputValid = false;
            UpdateButtonFunctionality();
            return;
        }

        // Clear validation error if validation passes
        ValidationError = string.Empty;
        HasValidationError = false;
        IsValid = true;
        IsInputValid = true;

        // Proceed with initialization and GUI update
        Initialize(value, SolutionMode, DisplayMode);
        UpdateGui();
        UpdateButtonFunctionality();
    }

    partial void OnSolutionModeChanged(SolutionMode oldValue, SolutionMode newValue)
    {
        // Validate the board size again when the solution mode changes
        var validationResult = InputViewModel.Validate(this);

        if (!validationResult.IsValid)
        {
            // Handle validation failure (e.g., display an error message)
            ValidationError = validationResult.Errors.First().ErrorMessage;
            HasValidationError = true;
            IsValid = false;
            IsInputValid = false;
            UpdateButtonFunctionality();
            return;
        }

        // Clear validation error if validation passes
        ValidationError = string.Empty;
        HasValidationError = false;
        IsValid = true;
        IsInputValid = true;

        // If validation passes, proceed with initialization and GUI update
        Initialize(BoardSize, newValue, DisplayMode);
        UpdateGui();
        UpdateButtonFunctionality();
    }

    partial void OnDisplayModeChanged(DisplayMode oldValue, DisplayMode newValue)
    {
        Validate();
        UpdateButtonFunctionality();
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
        var validationResult = InputViewModel.Validate(this);
        IsValid = validationResult.IsValid;
        IsInputValid = validationResult.IsValid;
        UpdateButtonFunctionality();
    }

    private readonly EventManager _eventManager;
    private ICommandManager _commandManager;
    private bool _disposed;
}
