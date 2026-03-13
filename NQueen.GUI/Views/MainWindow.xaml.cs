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

    private void UpdateChessboardAndRelatedUI(ChessboardUserControl chessBoard)
    {
        var grid = (Grid)Content;

        // Compute available height by subtracting header height and the grid's top/bottom margins
        var rootMargin = ((FrameworkElement)grid).Margin;
        var headerBorder = (Border?)FindName("HeaderBorder");
        var headerHeight = headerBorder is not null
            ? headerBorder.ActualHeight + headerBorder.Margin.Bottom
            : 0.0;
        // Use the actual height of the content row (accounts for right panel needs and layout rounding)
        var rowHeight = grid.RowDefinitions[1].ActualHeight;

        // Compute available width for the center column (auto) by measuring available space
        // between left and right columns within the root grid
        // With SizeToContent=Width and center Auto, grid width equals sum of columns + spacers
        var leftWidth = grid.ColumnDefinitions[0].ActualWidth;
        var rightWidth = grid.ColumnDefinitions[4].ActualWidth; // fixed 400
        var spacerWidth = grid.ColumnDefinitions[1].ActualWidth + grid.ColumnDefinitions[3].ActualWidth; // 10 + 10
        var layoutAvailable = Math.Max(0, grid.ActualWidth - leftWidth - rightWidth - spacerWidth);

        // Subtract the left and right margin of the chessboardPlaceholder dynamically
        var totalHorizontalMargin = chessboardPlaceholder.Margin.Left + chessboardPlaceholder.Margin.Right;
        var availableWidth = Math.Max(0, layoutAvailable - totalHorizontalMargin);

        // Target a square board driven by available vertical height
        var targetBoardSize = rowHeight;

        // Apply a smaller initial cap so initial squares fit comfortably
        if (!_initialChessboardSized)
        {
            const double initialMax = 600.0; // tweak as needed
            targetBoardSize = Math.Min(targetBoardSize, initialMax);
            _initialChessboardSized = true;
        }

        // Keep window width constant based on content so gaps stay 10px
        // Width = margins + left column + spacer(10) + board + spacer(10) + right column(400)
        var desiredWindowWidth = rootMargin.Left + rootMargin.Right
            + leftWidth + grid.ColumnDefinitions[1].ActualWidth
            + targetBoardSize + grid.ColumnDefinitions[3].ActualWidth
            + rightWidth;
        if (!double.IsNaN(desiredWindowWidth) && desiredWindowWidth > 0)
        {
            Width = desiredWindowWidth;
        }

        // Set chessboard to the targeted square size; container will constrain if needed
        chessBoard.Width = targetBoardSize;
        chessBoard.Height = targetBoardSize;

        // Set the height of the solution list to match the chessboard
        solutionList.Height = targetBoardSize;

        MainViewModel.ChessboardVm.WindowWidth = chessBoard.ActualWidth;
        MainViewModel.ChessboardVm.WindowHeight = chessBoard.ActualHeight;

        MainViewModel.ResetChessboard(targetBoardSize);

        // (width already set above using targetBoardSize)
    }


    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
    private bool _initialChessboardSized = false;
}
