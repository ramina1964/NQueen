namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
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

        // Set BoundingRectangle and IsOffscreen for each square
        foreach (var square in chessboardViewModel.Squares)
        {
            var boundingRect = new Rect(
                square.Position.ColumnNo * square.Width,
                square.Position.RowNo * square.Height,
                square.Width,
                square.Height);

            square.BoundingRectangle = boundingRect;

            // Determine if the square is off-screen
            square.IsOffscreen =
                boundingRect.Right < 0 ||
                boundingRect.Bottom < 0 ||
                boundingRect.Left > chessboardViewModel.WindowWidth ||
                boundingRect.Top > chessboardViewModel.WindowHeight;
        }
    }
}
