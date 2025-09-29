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

    [ObservableProperty]
    private bool _enableParallelization = true;

    // When true (primarily in unit tests), suppress all modal MessageBox dialogs.
    [ObservableProperty]
    private bool _suppressUserDialogs;
}
