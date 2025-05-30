namespace NQueen.GUI.Views;

// Todo: Make height of these controls equal: SolutionListUserControl and ChessboardUserControl.
// Todo: Align top of these controls: SolutionListUserControl and ChessboardUserControl.
public partial class MainView : Window, IDisposable
{
    public MainView(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
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
            DataContext = MainViewModel;
        }
    }

    private void MainView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (chessboardPlaceholder.Content is ChessboardUserControl board)
        {
            // Get the available height for the row
            var rowHeight = ((Grid)Content).RowDefinitions[1].ActualHeight;
            // Get the available width for the column
            var colWidth = ((Grid)Content).ColumnDefinitions[1].ActualWidth;

            // The chessboard should be square, so use the smaller of the two
            var size = Math.Min(rowHeight, colWidth);

            board.Width = size;
            board.Height = size;

            MainViewModel.ChessboardVm.WindowWidth = board.ActualWidth;
            MainViewModel.ChessboardVm.WindowHeight = board.ActualHeight;

            MainViewModel.SetChessboard(size);
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
