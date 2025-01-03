﻿namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
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
    public ObservableCollection<Solution> _observableSolutions = new();

    [ObservableProperty]
    private Solution _selectedSolution;

    [ObservableProperty]
    private SolutionMode _solutionMode;

    [ObservableProperty]
    private DisplayMode _displayMode;

    [ObservableProperty]
    private byte _boardSize;

    public string ResultTitle => SolutionHelper.SolutionTitle(SolutionMode);

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

    private bool _disposed;

    private CancellationTokenSource CancelationTokenSource { get; set; }

    private readonly ISolver Solver;

    partial void OnSelectedSolutionChanged(Solution value)
    {
        if (value != null)
        {
            Chessboard.PlaceQueens(value.Positions);

            // Call DisplaySolution on ChessboardUserControl
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainView mainView)
                {
                    var chessboardUserControl = mainView.FindName("ChessboardControl") as ChessboardUserControl;
                    chessboardUserControl?.DisplaySolution(value);
                }
            });
        }
    }
}
