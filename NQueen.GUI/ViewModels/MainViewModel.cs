namespace NQueen.GUI.ViewModels;

public sealed class MainViewModel : ObservableObject, IDataErrorInfo, IDisposable
{
    public MainViewModel(ISolver solver)
    {
        Initialize(solver);
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

    #region IDataErrorInfo
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
    #endregion IDataErrorInfo

    #region PublicProperties
    public IAsyncRelayCommand SimulateCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public RelayCommand SaveCommand { get; set; }

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            SetProperty(ref _progressValue, value);
            ProgressLabel = $"{_progressValue} %";
        }
    }

    public string ProgressLabel
    {
        get => _progressLabel;
        set => SetProperty(ref _progressLabel, value);
    }

    public Visibility ProgressVisibility
    {
        get => _progressVisibility;
        set
        {
            _ = SetProperty(ref _progressVisibility, value);
            if (DisplayMode == DisplayMode.Visualize)
            {
                OnPropertyChanged(nameof(ProgressLabel));
            }
        }
    }

    public Visibility ProgressLabelVisibility
    {
        get => _progressLabelVisibility;
        set => SetProperty(ref _progressLabelVisibility, value);
    }

    public IEnumerable<SolutionMode> SolutionModeList
    {
        get => Enum.GetValues(typeof(SolutionMode)).Cast<SolutionMode>();
        set => SetProperty(ref _enumSolutionModes, value);
    }

    public IEnumerable<DisplayMode> DisplayModeList
    {
        get => Enum.GetValues(typeof(DisplayMode)).Cast<DisplayMode>();
        set => SetProperty(ref _enumDisplayModes, value);
    }

    public bool IsVisualized
    {
        get => _isVisualized;
        set => SetProperty(ref _isVisualized, value);
    }

    public int DelayInMilliseconds
    {
        get => _delayInMilliseconds;
        set
        {
            SetProperty(ref _delayInMilliseconds, value);
            Solver.DelayInMilliseconds = value;
        }
    }

    public SimulationResults SimulationResults
    {
        get => _simulationResults;
        set => SetProperty(ref _simulationResults, value);
    }

    public ObservableCollection<Solution> ObservableSolutions { get; set; }

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
    }

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

    public string BoardSizeText
    {
        get => _boardSizeText;
        set
        {
            if (!SetProperty(ref _boardSizeText, value))
            { return; }
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
                SetProperty(ref _boardSize, sbyte.Parse(value));
                OnPropertyChanged(nameof(BoardSize));

                UpdateButtonFunctionality();
                UpdateGui();
            }
        }
    }

    public sbyte BoardSize
    {
        get => _boardSize;
        set => SetProperty(ref _boardSize, value);
    }

    public string ResultTitle => Utility.SolutionTitle(SolutionMode);

    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    public ISolver Solver
    {
        get => _solver;
        set => SetProperty(ref _solver, value);
    }

    public string SolutionTitle
    {
        get => _solutionTitle;
        set => SetProperty(ref _solutionTitle, value);
    }

    public string NoOfSolutions
    {
        get => _noOfSoltions;
        set
        {
            if (SetProperty(ref _noOfSoltions, value))
            { OnPropertyChanged(nameof(ResultTitle)); }
        }
    }

    public string MemoryUsage
    {
        get => _memoryUsage;
        set => SetProperty(ref _memoryUsage, value);
    }

    // Todo: Show application's memory usage in MB/GB based on its value being under/over 1GB.
    public void UpdateMemoryUsage()
    {
        const double MB = 1024.0 * 1024;
        const double GB = MB * 1024;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var memoryUsageInMB = memoryUsageInBytes / MB;
        var memoryUsageInGB = memoryUsageInBytes / GB;

        MemoryUsage = memoryUsageInGB >= 1
            ? $"{memoryUsageInGB:F2} GB"
            : $"{memoryUsageInMB:F2} MB";
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

    public string ElapsedTimeInSec
    {
        get => _elapsedTime;
        set => SetProperty(ref _elapsedTime, value);
    }

    // Returns true if a simulation is running, false otherwise. SolutionMode could be all of the three enum values.
    public bool IsSimulating
    {
        get => _isSimulating;
        set
        {
            if (SetProperty(ref _isSimulating, value))
            { UpdateButtonFunctionality(); }
        }
    }

    public bool IsInInputMode
    {
        get => _isInInputMode;
        set
        {
            if (SetProperty(ref _isInInputMode, value))
            { UpdateButtonFunctionality(); }
        }
    }

    public bool IsSingleRunning
    {
        get => _isSingleRunning;
        set => SetProperty(ref _isSingleRunning, value);
    }

    public bool IsIdle
    {
        get => _isIdle;
        set
        {
            if (SetProperty(ref _isIdle, value))
            { UpdateButtonFunctionality(); }
        }
    }

    public bool IsOutputReady
    {
        get => _isOutputReady;
        set
        {
            if (SetProperty(ref _isOutputReady, value))
            { UpdateButtonFunctionality(); }
        }
    }

    #endregion PublicProperties

    #region PrivateMethods

    private InputViewModel InputViewModel { get; set; }

    private void Initialize(ISolver solver, sbyte boardSize = Utility.DefaultBoardSize,
        SolutionMode solutionMode = Utility.DefaultSolutionMode,
        DisplayMode displayMode = Utility.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel { ClassLevelCascadeMode = CascadeMode.Stop };
        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        SaveCommand = new RelayCommand(Save, CanSave);

        Solver = solver;
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

    private void ReleaseResources(SimulationStatus simulationStatus)
    {
        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsSingleRunning = false;
        IsOutputReady = true;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;

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

                // If SolutionMode.Unique || SolutionMode.All
                else if (IsSimulating)
                {
                    IsSingleRunning = false;
                    ProgressLabelVisibility = Visibility.Visible;
                    ProgressValue = Utility.StartProgressValue;
                }
                break;

            case SimulationStatus.Finished:
                UnsubscribeFromSimulationEvents();

                break;
        }
    }

    private void SubscribeToSimulationEvents()
    {
        Solver.ProgressValueChanged += OnProgressValueChanged;
        Solver.QueenPlaced += OnQueenPlaced;
        Solver.SolutionFound += OnSolutionFound;
    }

    private void UnsubscribeFromSimulationEvents()
    {
        Solver.QueenPlaced -= OnQueenPlaced;
        Solver.SolutionFound -= OnSolutionFound;
        Solver.ProgressValueChanged -= OnProgressValueChanged;
    }

    private void UpdateButtonFunctionality()
    {
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValue = e.Value;

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution([.. e.Solution], 1);
        var positions = sol
            .QueenList.Where(q => q > -1)
            .Select((item, index) => new Position((sbyte)index, item)).ToList();

        Chessboard.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = ObservableSolutions.Count + 1;
        var sol = new Solution([.. e.Solution], id);

        _ = Application
            .Current
            .Dispatcher
            .BeginInvoke(DispatcherPriority.Send, new Action(() => ObservableSolutions.Add(sol)));

        SelectedSolution = sol;
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

    private async Task SimulateAsync()
    {
        ReleaseResources(SimulationStatus.Started);

        UpdateGui();
        SimulationResults = await Solver.GetResultsAsync(BoardSize, SolutionMode, DisplayMode);

        ExtractCorrectNoOfSols();
        NoOfSolutions = $"{SimulationResults.NoOfSolutions,0:N0}";
        ElapsedTimeInSec = $"{SimulationResults.ElapsedTimeInSec,0:N1}";
        SelectedSolution = ObservableSolutions.FirstOrDefault();

        // Update memory usage after the simulation process completes
        UpdateMemoryUsage();

        ReleaseResources(SimulationStatus.Finished);
    }

    private bool CanSimulate() => IsValid && IsIdle;

    private void Cancel() => Solver.IsSolverCanceled = true;

    private bool CanCancel() => IsSimulating;

    private void Save()
    {
        var results = new ResultPresentation(SimulationResults);
        var filePath = results.Write2File(SolutionMode);
        var msg = $"Successfully wrote results to: {filePath}";
        MessageBox.Show(msg);
        IsIdle = true;
    }

    private bool CanSave() => IsIdle && IsOutputReady;
    #endregion PrivateMethods

    #region PrivateFields
    private double _progressValue;
    private string _progressLabel;
    private Visibility _progressLabelVisibility;
    private Visibility _progressVisibility;
    private IEnumerable<SolutionMode> _enumSolutionModes;
    private IEnumerable<DisplayMode> _enumDisplayModes;
    private static SimulationResults _simulationResults;
    private int _delayInMilliseconds;
    private string _noOfSoltions;
    private string _elapsedTime;
    private SolutionMode _solutionMode;
    private DisplayMode _displayMode;
    private string _boardSizeText;
    private sbyte _boardSize;
    private bool _isVisualized;
    private bool _isValid;
    private bool _isSingleRunning;
    private bool _isIdle;
    private bool _isSimulating;
    private bool _isInInputMode;
    private bool _isOutputReady;
    private ISolver _solver;
    private Solution _selectedSolution;
    private string _solutionTitle;
    private string _memoryUsage;

    #endregion PrivateFields
}
