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

    // New: storage mode enum options
    [ObservableProperty]
    private IEnumerable<ResultStorageMode> _enumStorageModes =
        Enum.GetValues<ResultStorageMode>().Cast<ResultStorageMode>();

    // Selected storage mode per solution category (UI bound)
    [ObservableProperty]
    private ResultStorageMode _selectedAllStorageMode = SimulationSettings.DefaultAllStorageMode;

    [ObservableProperty]
    private ResultStorageMode _selectedUniqueStorageMode = SimulationSettings.DefaultUniqueStorageMode;

    [ObservableProperty]
    private bool _isVisualized;

    [ObservableProperty]
    private int _delayInMilliseconds;

    partial void OnDelayInMillisecondsChanged(int value)
    {
        if (_solver != null)
            _solver.DelayInMillisec = value;
    }

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
    [NotifyPropertyChangedFor(nameof(ResultTitle))]
    private string _noOfSolutions = "0";

    [ObservableProperty]
    private string _memoryUsage = "0";

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

    // Removed legacy IsCountOnlyUniqueMode property from this partial to avoid duplication (now kept in main file as legacy comment)

    // When true (primarily in unit tests), suppress all modal MessageBox dialogs.
    [ObservableProperty]
    private bool _suppressUserDialogs;

    // -------- Advanced / Performance Tunables --------

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
        if (value < 1) ParallelRootSplitDepth = 1; // clamp
        else if (_solver is NQueen.Kernel.Solvers.BitmaskSolver b)
            b.ParallelRootSplitDepth = value;
    }

    // Auto-tuning flag (true => user cannot modify parallel settings directly)
    private bool _autoTuneParallel = true;

    // Hooks consolidated into validation partial; remove duplicate implementations here.
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
    }
}
