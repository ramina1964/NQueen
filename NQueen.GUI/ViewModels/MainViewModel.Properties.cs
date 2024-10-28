using NQueen.Kernel.Enums;
using NQueen.Kernel.Models;
using NQueen.Kernel.Utilities;

namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
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
            _solver.DelayInMilliseconds = value;
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
            if (_solver == null || !isChanged)
            { return; }

            SolutionTitle =
                (SolutionMode == SolutionMode.Single)
                ? $"Solution"
                : $"Solutions (Max: {Utility.MaxNoOfSolutionsInOutput})";

            OnPropertyChanged(nameof(BoardSizeText));
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
}
