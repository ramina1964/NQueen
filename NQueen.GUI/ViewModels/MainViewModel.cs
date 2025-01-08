namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable, IDataErrorInfo
{
    // Constructors
    #region Constructors

    public MainViewModel(ISolver solver, ICommandManager commandManager)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));
        CommandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));

        ObservableSolutions = new ObservableCollection<Solution>();
        Initialize();
        SubscribeToSimulationEvents();
        CommandManager.Initialize(this);
    }

    #endregion Constructors

    // Property Injection for CommandManager
    public ICommandManager CommandManager
    {
        get => _commandManager;
        set
        {
            _commandManager = value;
            _commandManager?.Initialize(this); // Ensure CommandManager is initialized
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
        ElapsedTimeInSec = $"{0,0:N1}";
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
                SubscribeToSimulationEvents();

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
                UnsubscribeFromSimulationEvents();

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
    #endregion Methods

    // Event Handlers
    #region Event Handlers
    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValue = e.Value;

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution(e.Solution, 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        NoOfSolutions = $"{int.Parse(NoOfSolutions) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                ObservableSolutions.RemoveAt(0);
            }
            if (ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                ObservableSolutions.Add(sol);
            }
        }));

        SelectedSolution = sol;
    }

    private void SubscribeToSimulationEvents()
    {
        Solver.ProgressValueChanged += OnProgressValueChanged;
        Solver.QueenPlaced += OnQueenPlaced;
        Solver.SolutionFound += OnSolutionFound;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        Solver.ProgressValueChanged -= OnProgressValueChanged;
        Solver.QueenPlaced -= OnQueenPlaced;
        Solver.SolutionFound -= OnSolutionFound;
    }
    #endregion Event Handlers

    // Partial Methods
    #region Partial Methods
    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            Chessboard?.PlaceQueens(value.Positions);

            // Call DisplaySolution on ChessboardUserControl
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainView mainView)
                {
                    var chessboardUserControl = mainView.FindName("ChessboardControl") as ChessboardUserControl;
                    chessboardUserControl?.DisplaySolution(value.Positions);
                }
            });
        }
    }

    partial void OnProgressValueChanged(double value) => ProgressLabel = $"{value} %";

    partial void OnProgressVisibilityChanged(Visibility value)
    {
        IsProgressBarOffscreen = value != Visibility.Visible;
        if (DisplayMode == DisplayMode.Visualize)
        {
            OnPropertyChanged(nameof(ProgressLabel));
        }
    }

    partial void OnProgressLabelVisibilityChanged(Visibility value) =>
        IsProgressLabelOffscreen = value != Visibility.Visible;

    partial void OnDelayInMillisecondsChanged(int value) =>
        Solver.DelayInMilliseconds = value;

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        if (Solver == null)
        {
            return;
        }

        SolutionTitle = (value == SolutionMode.Single)
            ? $"Solution"
            : $"Solutions (Max: {SolutionHelper.MaxNoOfSolutionsInOutput})";

        OnPropertyChanged(nameof(BoardSize));
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

    partial void OnBoardSizeChanged(byte value)
    {
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
            UpdateButtonFunctionality();
            UpdateGui();
        }
    }

    partial void OnNoOfSolutionsChanged(string value) =>
        OnPropertyChanged(nameof(ResultTitle));

    partial void OnIsSimulatingChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsInInputModeChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsIdleChanged(bool value) => UpdateButtonFunctionality();

    partial void OnIsOutputReadyChanged(bool value) => UpdateButtonFunctionality();
    #endregion Partial Methods

    private ICommandManager _commandManager;
}
