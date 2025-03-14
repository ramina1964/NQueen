namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel ??
            throw new ArgumentNullException(nameof(mainViewModel));
    }

    public void DisplaySolution(List<Position> positions)
    {
        if (positions == null || positions.Count == 0)
            return;

        if (DataContext is not MainViewModel mainViewModel)
            return;

        var chessboardViewModel = mainViewModel.Chessboard;
        if (chessboardViewModel == null)
            return;

        chessboardViewModel.PlaceQueens(positions);
    }
}
 