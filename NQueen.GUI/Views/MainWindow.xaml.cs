namespace NQueen.GUI.Views;

public partial class MainWindow : Window, IDisposable
{
    public MainWindow(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
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

        // Row 1 (* row) gives the available height for the board
        var rowHeight = grid.RowDefinitions[1].ActualHeight;

        // Column 0 (solution list) and Column 2 (chessboard) are kept equal width at all times.
        // The available horizontal space minus the two 10-px spacers and the 400-px right panel
        // is split evenly between them.  Column 5 (Width="*") absorbs any remainder so that
        // extra space always appears after the right panel, not between columns.
        var rightWidth = grid.ColumnDefinitions[4].ActualWidth; // fixed 400
        var spacerWidth = grid.ColumnDefinitions[1].ActualWidth + grid.ColumnDefinitions[3].ActualWidth; // 10 + 10
        var maxBoardSizeFromWidth = Math.Max(0, (grid.ActualWidth - spacerWidth - rightWidth) / 2.0);

        // Guard: layout not ready yet (window still initialising)
        if (maxBoardSizeFromWidth <= 0 || rowHeight <= 0)
            return;

        // Square board bounded by both available height and half the available horizontal space
        var targetBoardSize = Math.Min(rowHeight, maxBoardSizeFromWidth);

        // Size the chessboard and the solution-list panel identically
        chessBoard.Width = targetBoardSize;
        chessBoard.Height = targetBoardSize;
        solutionList.Width = targetBoardSize;
        solutionList.Height = targetBoardSize;

        MainViewModel.ChessboardVm.WindowWidth = chessBoard.ActualWidth;
        MainViewModel.ChessboardVm.WindowHeight = chessBoard.ActualHeight;

        MainViewModel.ResetChessboard(targetBoardSize);
    }


    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
