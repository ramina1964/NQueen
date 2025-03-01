namespace NQueen.GUI.Views;

public partial class MainView : Window, IDisposable
{
    public MainView(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
        MainViewModel = mainViewModel;
        _serviceProvider = serviceProvider;

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

                // Unsubscribe from the Loaded event
                Loaded -= MainView_Loaded;
            }
        }

        // Clean up any unmanaged resources here
        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        var board = Chessboard.Content as ChessboardUserControl;
        var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
        board.Width = size;
        board.Height = size;
        MainViewModel.SetChessboard(size);
        DataContext = MainViewModel;
    }

    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
