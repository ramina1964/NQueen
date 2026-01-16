namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _boardSizeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressLabel))]
    private double _progressValue = 0;

    private void UpdateProgress(double value, string label)
    {
        value = Math.Clamp(value, 0, 1);
        _uiDispatcher.Invoke(() =>
        {
            Debug.WriteLine($"[MainViewModel] ProgressValue set to: {value}");
            ProgressValue = value;
            ProgressLabel = label;
        });
    }

    // Disable DisplayMode and Delay editing while simulating
    public bool CanEditDisplayMode => IsInInputMode && !IsSimulating;
    public bool CanEditDelay => IsInInputMode && !IsSimulating;

    [ObservableProperty]
    private string _progressLabel = string.Empty;

    [ObservableProperty]
    private Visibility _progressVisibility = Visibility.Hidden;

    partial void OnProgressVisibilityChanged(Visibility value) =>
        IsProgressBarOffscreen = value != Visibility.Visible;

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
    private IEnumerable<ResultStorageMode> _enumStorageModes =
        Enum.GetValues<ResultStorageMode>().Cast<ResultStorageMode>();

    private ResultStorageMode _allStorageMode = SimulationSettings.DefaultAllStorageMode;
    private ResultStorageMode _uniqueStorageMode = SimulationSettings.DefaultUniqueStorageMode;

    public ResultStorageMode SelectedStorageMode
    {
        get
        {
            if (IsVisualized) return ResultStorageMode.Materialize;
            return SolutionMode switch
            {
                SolutionMode.All => _allStorageMode,
                SolutionMode.Unique => _uniqueStorageMode,
                SolutionMode.Single => _allStorageMode,
                _ => _allStorageMode
            };
        }
        set
        {
            if (IsVisualized) return;
            var changed = false;
            switch (SolutionMode)
            {
                case SolutionMode.All:
                    if (_allStorageMode != value) { _allStorageMode = value; changed = true; }
                    break;
                case SolutionMode.Unique:
                    if (_uniqueStorageMode != value) { _uniqueStorageMode = value; changed = true; }
                    break;
                case SolutionMode.Single:
                    if (_allStorageMode != value) { _allStorageMode = value; changed = true; }
                    break;
            }
            if (changed)
            {
                OnPropertyChanged();
                ApplyStorageModesToSolver();
            }
        }
    }

    private void ApplyStorageModesToSolver()
    {
        if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
        {
            b.AllStorageMode = _allStorageMode;
            b.UniqueStorageMode = _uniqueStorageMode;
        }
    }

    [ObservableProperty]
    private bool _isVisualized;

    [ObservableProperty]
    private int _delayInMilliseconds; // Implementation of OnDelayInMillisecondsChanged moved to Events partial to avoid duplicate

    [ObservableProperty]
    private SimulationResults _simulationResults = new([], 0.0);

    [ObservableProperty]
    private ObservableCollection<Solution> _observableSolutions = [];

    [ObservableProperty]
    private Solution _selectedSolution = new([0], new DefaultSolutionFormatter());

    [ObservableProperty]
    private SolutionMode _solutionMode;

    [ObservableProperty]
    private DisplayMode _displayMode;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private string _solutionTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultLabel))]
    private string _noOfSolutions = "0";

    [ObservableProperty]
    private string _memoryConsumption = "0";

    public string MemoryUsage => MemoryConsumption;
    partial void OnMemoryConsumptionChanged(string value) => OnPropertyChanged(nameof(MemoryUsage));

    [ObservableProperty]
    private string _elapsedTimeInSec = string.Empty;

    [ObservableProperty]
    private bool _isSimulating;

    partial void OnIsSimulatingChanged(bool value)
    {
        RefreshCommandStates();
        OnPropertyChanged(nameof(CanEditDisplayMode));
        OnPropertyChanged(nameof(CanEditDelay));
    }

    [ObservableProperty]
    private bool _isInInputMode;

    partial void OnIsInInputModeChanged(bool value)
    {
        RefreshCommandStates();
        OnPropertyChanged(nameof(CanChangeStorageMode));
        OnPropertyChanged(nameof(CanEditDisplayMode));
        OnPropertyChanged(nameof(CanEditDelay));
    }

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

    [ObservableProperty]
    private bool _suppressUserDialogs;

    [ObservableProperty]
    private bool _useParallel = SimulationSettings.DefaultUseParallel;
    partial void OnUseParallelChanged(bool value)
    {
        if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
            b.UseParallel = value;
    }

    [ObservableProperty]
    private int _parallelRootSplitDepth = SimulationSettings.DefaultParallelRootSplitDepth;
    partial void OnParallelRootSplitDepthChanged(int value)
    {
        if (value < 1) ParallelRootSplitDepth = 1;
        else if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
            b.ParallelRootSplitDepth = value;
    }

    private readonly bool _autoTuneParallel = true;

    private void AutoAdjustParallel()
    {
        if (!_autoTuneParallel || _solver is not NQueen.Kernel.Solvers.BitmaskSolver bs)
            return;
        if (!ParsingUtils.TryParseInt(BoardSizeText, out var n))
            return;

        bool parallel;
        if (DisplayMode == DisplayMode.Visualize)
            parallel = false;
        else if (SolutionMode == SolutionMode.Single)
            parallel = n >= 14;
        else
            parallel = n >= 9;

        bs.UseParallel = parallel;
        UseParallel = parallel;

        int depth;
        if (!parallel) depth = 1;
        else if (n < 12) depth = 1;
        else if (n < 16) depth = 2;
        else depth = 3;
        if (depth > n) depth = n < 1 ? 1 : n;
        bs.ParallelRootSplitDepth = depth;
        ParallelRootSplitDepth = depth;
        bs.EnableHalfBoardRestriction = ComputeHalfBoardRestriction();
        OnPropertyChanged(nameof(EnableHalfBoardRestriction));
    }

    private bool ComputeHalfBoardRestriction()
    {
        if (!ParsingUtils.TryParseInt(BoardSizeText, out var n)) return false;
        return SolutionMode == SolutionMode.All && DisplayMode != DisplayMode.Visualize && n >= 15;
    }

    public bool EnableHalfBoardRestriction
    {
        get => ComputeHalfBoardRestriction();
        set
        {
            var auto = ComputeHalfBoardRestriction();
            if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
                b.EnableHalfBoardRestriction = auto;
            OnPropertyChanged();
        }
    }

    public bool CanChangeStorageMode => !IsVisualized && IsInInputMode;

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value == null || ChessboardVm == null) return;
        // Stop any ongoing visualization timer to avoid overwriting user selection
        StopVisualizationTimer();
        // Ensure board squares match the solution's board size
        var n = value.BoardSize;
        if (ChessboardVm.Squares.Count == 0 || !ChessboardVm.IsBoardStateUpdatedAndSquaresPopulated(n))
        {
            ChessboardVm.CreateSquares(n);
        }
        ChessboardVm.PlaceQueens(value.Positions);
    }
}
