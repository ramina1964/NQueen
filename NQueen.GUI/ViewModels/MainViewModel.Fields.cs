namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
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
