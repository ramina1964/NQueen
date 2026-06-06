namespace NQueen.GUI.Views;

public partial class MainWindow : Window, IDisposable
{
    // Fixed design board dimension. The Viewbox in MainWindow.xaml scales this to the
    // actual window size, so the layout is composed once and then GPU-scaled — the board
    // stays square and the whole UI zooms uniformly without per-resize recomputation.
    private const double DesignBoardSize = 640;

    public MainWindow(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        DataContext = mainViewModel
            ?? throw new ArgumentNullException(nameof(mainViewModel));

        Loaded += MainView_Loaded;
        MainViewModel = mainViewModel;

        // Resolve and add ChessboardUserControl to the MainWindow
        var chessboard = _serviceProvider.GetRequiredService<ChessboardUserControl>();
        chessboard.DataContext = MainViewModel;
        chessboardPlaceholder.Content = chessboard;

        // Resolve and add InputPanelUserControl to the MainWindow
        var inputPanel = _serviceProvider.GetRequiredService<InputPanelUserControl>();
        inputPanel.DataContext = MainViewModel;
        inputPanelPlaceHolder.Content = inputPanel;

        // Resolve and add SimulationPanelUserControl to the MainWindow
        var simulationPanel = _serviceProvider.GetRequiredService<SimulationPanelUserControl>();
        simulationPanel.DataContext = MainViewModel;
        simulationPanelPlaceHolder.Content = simulationPanel;
    }

    public MainViewModel MainViewModel { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_disposed == false)
        {
            Dispose(true);
            _disposed = true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (MainViewModel != null)
            {
                MainViewModel.Dispose();
                Loaded -= MainView_Loaded;
                MainViewModel = null!;
            }
        }

        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        if (chessboardPlaceholder.Content is not ChessboardUserControl board)
            throw new InvalidOperationException(
                "chessboardPlaceholder.Content is not a ChessboardUserControl.");

        ApplyDesignLayout(board);
    }

    /// <summary>
    /// Composes the layout once at a fixed design size. The Viewbox in MainWindow.xaml
    /// scales the result to the actual window, so the board stays square and the whole
    /// UI zooms uniformly — no monitor-fit arithmetic or per-resize recomputation needed.
    /// </summary>
    private void ApplyDesignLayout(ChessboardUserControl chessBoard)
    {
        chessBoard.Width    = DesignBoardSize;
        chessBoard.Height   = DesignBoardSize;
        solutionList.Height = DesignBoardSize;

        // Keep the ViewModel dimensions in sync with the design board.
        MainViewModel.ChessboardVm.WindowWidth  = DesignBoardSize;
        MainViewModel.ChessboardVm.WindowHeight = DesignBoardSize;

        // Only clear and rebuild squares when idle — never interrupt an active simulation.
        if (!MainViewModel.IsSimulating)
            MainViewModel.ResetChessboard(DesignBoardSize);
    }

    // --- Private fields ---
    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
