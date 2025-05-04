namespace NQueen.GUI.Views;

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
        var chessboardUserControl = _serviceProvider.GetRequiredService<ChessboardUserControl>();
        chessboardPlaceholder.Content = chessboardUserControl;

        // Resolve and add InputPanelUserControl to the MainView
        var inputPanel = _serviceProvider.GetRequiredService<InputPanelUserControl>();
        inputPanelPlaceHolder.Content = inputPanel;

        // Resolve and add SimulationPanelUserControl to the MainView
        var simulationPanelUserControl = _serviceProvider.GetRequiredService<SimulationPanelUserControl>();
        simulationPanelPlaceHolder.Content = simulationPanelUserControl;
    }

    public MainViewModel MainViewModel { get; }

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
            UpdateChessboardSize(board);
            board.SizeChanged += (s, args) => UpdateChessboardSize(board);
            DataContext = MainViewModel;
        }
    }

    private void MainView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (chessboardPlaceholder.Content is ChessboardUserControl board)
        {
            var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
            board.Width = size;
            board.Height = size;

            MainViewModel.ChessboardVm.WindowWidth = board.ActualWidth;
            MainViewModel.ChessboardVm.WindowHeight = board.ActualHeight;

            MainViewModel.SetChessboard(size);
        }
    }

    private void UpdateChessboardDimensions(ChessboardUserControl board)
    {
        var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
        board.Width = size;
        board.Height = size;

        // Update the ChessboardVm dimensions
        MainViewModel.ChessboardVm.WindowWidth = board.ActualWidth;
        MainViewModel.ChessboardVm.WindowHeight = board.ActualHeight;

        // Optionally, update the chessboard size in the ViewModel
        MainViewModel.SetChessboard(size);
    }

    private void UpdateChessboardSize(ChessboardUserControl board)
    {
        var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
        board.Width = size;
        board.Height = size;
        MainViewModel.SetChessboard(size);
    }

    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
