namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel :
    ObservableObject, INotifyDataErrorInfo, IDisposable
{
    // --- Constructor ---
    public MainViewModel(
        ISolver solver,
        IDispatcher dispatcher,
        ISaveFileDialogService saveFileService,
        ISolutionFormatter solutionFormatter)
    {
        _solver = solver ??
            throw new ArgumentNullException(nameof(solver));

        _uiDispatcher = dispatcher ??
            throw new ArgumentNullException(nameof(dispatcher));

        _saveFileService = saveFileService ??
            throw new ArgumentNullException(nameof(saveFileService));

        _solutionFormatter = solutionFormatter ??
            throw new ArgumentNullException(nameof(solutionFormatter));

        InputViewModel = new InputViewModel(SolutionMode.Unique);
        CancellationTokenSource = new CancellationTokenSource();
        ChessboardVm = new ChessboardViewModel(_uiDispatcher);

        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel, CanCancel);

        Initialize();
        SubscribeToSimulationEvents();
    }

    // --- Public Properties ---
    public int BoardSize =>
        ParsingUtils.TryParseInt(BoardSizeText, out var boardSize)
            ? boardSize
            : _lastValidBoardSize;

    public string ResultTitle =>
        SolutionFormatter.UpdateSolutionTitle(SolutionMode);

    public ChessboardViewModel ChessboardVm { get; set; }

    public IAsyncRelayCommand SimulateCommand { get; private set; }

    public IRelayCommand SaveCommand { get; private set; }

    public IRelayCommand CancelCommand { get; private set; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public event EventHandler? SimulationCompleted;

    public bool HasErrors => _errors.Count != 0;

    // --- Public Methods ---
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.Values
                .SelectMany(errors => errors);

        return _errors.TryGetValue(propertyName, out var propertyErrors)
            ? propertyErrors
            : Enumerable.Empty<string>();
    }

    public void ResetChessboard(double boardDimension)
    {
        // If BoardSizeText is invalid, do not clear the chessboard, just return
        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        // Validate board size for the current solution mode, if invalid, just return
        var validationResult = InputViewModel.ValidateBoardSize(BoardSizeText);
        if (validationResult.IsValid == false)
            return;

        // Explicitly clear any queen images before resetting the chessboard
        ChessboardVm.ClearImages();

        ChessboardVm.WindowWidth = boardDimension;
        ChessboardVm.WindowHeight = boardDimension;
        ChessboardVm.CreateSquares(boardSize);
        IsIdle = true;
        IsSimulating = false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // --- Private Methods ---
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CancellationTokenSource?.Dispose();
            ObservableSolutions.Clear();
            ChessboardVm?.Squares.Clear();
            UnsubscribeFromSimulationEvents();

            // Marking large objects for garbage collection
            CancellationTokenSource = null!;
            ChessboardVm = null!;
            InputViewModel = null!;
        }

        _disposed = true;
    }

    private void Initialize(
        int boardSize = BoardSettings.DefaultBoardSize,
        SolutionMode solutionMode = SimulationSettings.DefaultSolutionMode,
        DisplayMode displayMode = SimulationSettings.DefaultDisplayMode)
    {
        InputViewModel = new InputViewModel(solutionMode);
        BoardSizeText = boardSize.ToString();
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        // Explicitly validate BoardSizeText to ensure errors are captured.
        ValidateProperty(nameof(BoardSizeText));

        IsIdle = true;
        IsInInputMode = true;
        IsSimulating = false;
        IsOutputReady = false;
        NoOfSolutions = $"{ObservableSolutions.Count,0:N0}";

        DelayInMilliseconds = SimulationSettings.DefaultDelayInMilliseconds;
        ProgressVisibility = Visibility.Hidden;
        ProgressLabelVisibility = Visibility.Hidden;
    }

    // --- Private Fields ---
    private readonly Dictionary<string, List<string>> _errors = [];

    private int _lastValidBoardSize = BoardSettings.DefaultBoardSize;

    private InputViewModel InputViewModel { get; set; }

    private bool _disposed;

    private CancellationTokenSource CancellationTokenSource { get; set; }

    private readonly ISolver _solver;

    private readonly IDispatcher _uiDispatcher;

    private readonly ISaveFileDialogService _saveFileService;

    private readonly ISolutionFormatter _solutionFormatter;
}
