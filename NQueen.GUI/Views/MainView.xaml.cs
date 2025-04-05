namespace NQueen.GUI.Views;

public partial class MainView : Window, IDisposable
{
    public MainView(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
        SizeChanged += Window_SizeChanged;
        MainViewModel = mainViewModel
            ?? throw new ArgumentNullException(nameof(mainViewModel));

        _serviceProvider = serviceProvider ??
            throw new ArgumentNullException(nameof(serviceProvider));

        // Resolve and add ChessboardUserControl to the MainView
        var chessboardUserControl = new ChessboardUserControl(MainViewModel);
        Chessboard.Content = chessboardUserControl;

        // Resolve and add InputPanelUserControl to the MainView
        var inputPanelUserControl = _serviceProvider.GetRequiredService<InputPanelUserControl>();
        InputPanel.Content = inputPanelUserControl;

        // Resolve and add SimulationPanelUserControl to the MainView
        var simulationPanelUserControl = _serviceProvider.GetRequiredService<SimulationPanelUserControl>();
        SimulationPanel.Content = simulationPanelUserControl;
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
        Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose of any managed resources
            MainViewModel?.Dispose();

            // Unsubscribe from the Loaded event
            Loaded -= MainView_Loaded;
            SizeChanged -= Window_SizeChanged;
        }

        // Clean up any unmanaged resources here
        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Chessboard.Content is ChessboardUserControl board)
        {
            var size = Dimensions.ChessboardSquareSize(board.ActualWidth, board.ActualHeight, MainViewModel.BoardSize);
            MainViewModel.SetChessboard(size);
            DataContext = MainViewModel;
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.Chessboard != null)
        {
            viewModel.Chessboard.WindowWidth = e.NewSize.Width;
            viewModel.Chessboard.WindowHeight = e.NewSize.Height;

            // Update the squares with the new dimensions
            viewModel.Chessboard.CreateSquares(
                viewModel.BoardSize,
                viewModel.Chessboard.Squares,
                e.NewSize.Width,
                e.NewSize.Height);
        }
    }

    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
