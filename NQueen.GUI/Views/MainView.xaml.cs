namespace NQueen.GUI.Views;

// Todo: Consider using a more specific name for this class, such as MainWindowView
// or MainApplicationView.

// Todo: The error messages for the invalid board sizes have different length, so that changing
// the solution mode could cause widening/narrowing of the UserControl panels on the last column
// of the second row, i.e., Grid.Row = 1, Grid.Column = 3 of the MainView.xaml.
public partial class MainView : Window, IDisposable
{
    public MainView(MainViewModel mainViewModel, IServiceProvider serviceProvider)
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

        // Resolve and add ChessboardUserControl to the MainView
        var chessboard = _serviceProvider.GetRequiredService<ChessboardUserControl>();
        chessboard.DataContext = MainViewModel;
        chessboardPlaceholder.Content = chessboard;

        // Resolve and add InputPanelUserControl to the MainView
        var inputPanel = _serviceProvider.GetRequiredService<InputPanelUserControl>();
        inputPanel.DataContext = MainViewModel;
        inputPanelPlaceHolder.Content = inputPanel;

        // Resolve and add SimulationPanelUserControl to the MainView
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

    // Todo: Extract the common part of the following two methods into a separate one.
    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        if (chessboardPlaceholder.Content is not ChessboardUserControl board)
            throw new InvalidOperationException(
                "chessboardPlaceholder.Content is not a ChessboardUserControl.");
        else
        {
            UpdateChessboardSize(board);
            board.SizeChanged += (s, args) => UpdateChessboardSize(board);
        }
    }

    private void MainView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (chessboardPlaceholder.Content is ChessboardUserControl chessBoard)
        {
            var grid = (Grid)Content;

            // Get the available height for the row
            var rowHeight = grid.RowDefinitions[1].ActualHeight;

            // Get the available width for the column
            var colWidth = grid.ColumnDefinitions[1].ActualWidth;

            // Subtract the left and right margin of the chessboardPlaceholder (10 + 10)
            var chessboardMargin = 10; // if Margin="0,0,10,0" on both sides, use 10
            var availableWidth = colWidth - chessboardMargin;

            // The chessboard should be square, so use the smaller of the two
            var maxChessBoardSize = Math.Min(rowHeight, availableWidth);

            chessBoard.Width = maxChessBoardSize;
            chessBoard.Height = maxChessBoardSize;

            // Set the height of the solution list to match the chessboard
            solutionList.Height = maxChessBoardSize;

            MainViewModel.ChessboardVm.WindowWidth = chessBoard.ActualWidth;
            MainViewModel.ChessboardVm.WindowHeight = chessBoard.ActualHeight;

            MainViewModel.SetChessboard(maxChessBoardSize);
        }
    }

    private void UpdateChessboardSize(FrameworkElement board)
    {
        var availableDimensions = LayoutUtils.CalculateAvailableDimension(board);
        MainViewModel.SetChessboard(availableDimensions);
    }

    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
