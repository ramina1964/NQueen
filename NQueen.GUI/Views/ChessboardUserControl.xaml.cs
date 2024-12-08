namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        DataContext = _mainViewModel;

        // Subscribe to BoardSize changes
        _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
    }

    private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.BoardSize))
        {
            var boardSize = _mainViewModel.BoardSize;
            _mainViewModel.Chessboard.InitializeSquares(boardSize);
        }
    }

    private readonly MainViewModel _mainViewModel;
}
