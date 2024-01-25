namespace NQueen.GUI.Views;

public partial class MainView : Window, IDisposable
{
    public MainView(MainViewModel mainViewModel)
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
        MainViewModel = mainViewModel;
    }

    public MainViewModel MainViewModel { get; set; }

    public void Dispose()
    {

    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_disposed == false)
        {
            Dispose(true);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose of any managed resources
            MainViewModel?.Dispose();
        }

        // Clean up any unmanaged resources here

        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        var board = chessboard;
        var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
        board.Width = size;
        board.Height = size;
        MainViewModel.SetChessboard(size);
        DataContext = MainViewModel;
    }

    private bool _disposed = false;
}
