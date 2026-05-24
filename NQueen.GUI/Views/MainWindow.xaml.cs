namespace NQueen.GUI.Views;

public partial class MainWindow : Window, IDisposable
{
    public MainWindow(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        // Derive minimum window dimensions from the usable work area (screen minus taskbar).
        // Using 60% of the work area as a safe minimum ensures the window is always usable
        // on any monitor, including laptops, without ever overflowing the screen on startup.
        var workArea = SystemParameters.WorkArea;
        MinWidth  = Math.Round(workArea.Width  * 0.60);
        MinHeight = Math.Round(workArea.Height * 0.60);

        InitializeComponent();
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        DataContext = mainViewModel
            ?? throw new ArgumentNullException(nameof(mainViewModel));

        Loaded += MainView_Loaded;
        SizeChanged += MainView_SizeChanged;
        DpiChanged += MainView_DpiChanged;
        MainViewModel = mainViewModel;
        _serviceProvider = serviceProvider;

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
            // Dispose of any managed resources
            if (MainViewModel != null)
            {
                MainViewModel.Dispose();

                // Unsubscribe from events
                Loaded -= MainView_Loaded;
                SizeChanged -= MainView_SizeChanged;
                DpiChanged -= MainView_DpiChanged;

                MainViewModel = null!;
            }
        }

        // Clean up any unmanaged resources here
        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        if (chessboardPlaceholder.Content is not ChessboardUserControl board)
            throw new InvalidOperationException(
                "chessboardPlaceholder.Content is not a ChessboardUserControl.");
        else
        {
            // Initialize layout once at load. Further recalculations only happen on window resize.
            UpdateChessboardAndRelatedUI(board);
        }
    }

    private void MainView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Only recompute layout on window size changes, not on internal content changes
        if (e.WidthChanged || e.HeightChanged)
        {
            if (chessboardPlaceholder.Content is ChessboardUserControl chessBoard)
            {
                UpdateChessboardAndRelatedUI(chessBoard);
            }
        }
    }

    private void MainView_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        // Recalculate board geometry when the window is moved to a monitor with a different DPI
        if (chessboardPlaceholder.Content is ChessboardUserControl chessBoard)
            UpdateChessboardAndRelatedUI(chessBoard);
    }

    private void UpdateChessboardAndRelatedUI(ChessboardUserControl chessBoard)
    {
        var grid = (Grid)Content;

        // WPF owns Column 2 (Width="*") and allocates it all remaining space after the fixed
        // sibling columns. Simply read what WPF decided — no manual subtraction needed.
        var availableWidth  = grid.ColumnDefinitions[2].ActualWidth;
        var availableHeight = grid.RowDefinitions[1].ActualHeight;

        // Guard: layout not ready yet (window still initialising)
        if (availableWidth <= 0 || availableHeight <= 0)
            return;

        // Square board: take the smaller of the two available dimensions
        var targetBoardSize = Math.Min(availableHeight, availableWidth);

        // Size the chessboard; solution list height tracks the board
        chessBoard.Width  = targetBoardSize;
        chessBoard.Height = targetBoardSize;
        solutionList.Height = targetBoardSize;

        // Use targetBoardSize directly — ActualWidth/Height are stale until the next layout pass
        MainViewModel.ChessboardVm.WindowWidth  = targetBoardSize;
        MainViewModel.ChessboardVm.WindowHeight = targetBoardSize;

        MainViewModel.ResetChessboard(targetBoardSize);
    }


    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
